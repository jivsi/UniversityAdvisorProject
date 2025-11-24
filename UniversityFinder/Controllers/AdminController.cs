using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversityFinder.Data;
using UniversityFinder.Models;
using UniversityFinder.Services;

namespace UniversityFinder.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly HeiApiService _heiApiService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            HeiApiService heiApiService,
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _heiApiService = heiApiService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Sync()
        {
            var programsStatus = await _context.SyncStatuses
                .FirstOrDefaultAsync(s => s.SyncType == "Programs");

            ViewBag.ProgramsStatus = programsStatus;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncUniversities()
        {
            try
            {
                _logger.LogInformation("🔄 University sync requested by user {User}", User.Identity?.Name);
                var result = await _heiApiService.SyncUniversitiesAsync();
                TempData["SuccessMessage"] = $"✅ University sync complete! {result} universities added.";
                _logger.LogInformation("✅ University sync completed successfully. {Count} universities added.", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ University sync failed: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"❌ University sync failed: {ex.Message}. Check application logs for details.";
            }

            return RedirectToAction(nameof(Sync));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncPrograms()
        {
            try
            {
                _logger.LogInformation("🔄 Program sync requested by user {User}", User.Identity?.Name);
                
                // ✅ HARDENED: Check for stale running states and reset if needed
                await _heiApiService.CheckAndResetStaleSyncStatusesAsync();
                
                // ✅ HARDENED: Check if sync is already running with proper database query
                var existingStatus = await _context.SyncStatuses
                    .FirstOrDefaultAsync(s => s.SyncType == "Programs" && s.IsRunning);

                if (existingStatus != null)
                {
                    // Double-check: verify it's not a stale state (should be caught above, but extra safety)
                    if (existingStatus.StartedAt.HasValue)
                    {
                        var runningDuration = DateTime.UtcNow - existingStatus.StartedAt.Value;
                        if (runningDuration.TotalMinutes >= 30) // 30-minute timeout
                        {
                            _logger.LogWarning($"⚠ Detected stale running state (running for {runningDuration.TotalMinutes:F1} minutes). Resetting...");
                            existingStatus.IsRunning = false;
                            existingStatus.CompletedAt = DateTime.UtcNow;
                            existingStatus.LastMessage = "⚠ Sync was reset due to stale state";
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            TempData["InfoMessage"] = "⚠️ Program sync is already running in the background.";
                            return RedirectToAction(nameof(Sync));
                        }
                    }
                    else
                    {
                        // StartedAt is null but IsRunning is true - definitely stale, reset it
                        _logger.LogWarning("⚠ Detected invalid running state (StartedAt is null). Resetting...");
                        existingStatus.IsRunning = false;
                        existingStatus.CompletedAt = DateTime.UtcNow;
                        existingStatus.LastMessage = "⚠ Sync was reset due to invalid state";
                        await _context.SaveChangesAsync();
                    }
                }

                // Start background sync - returns immediately
                _heiApiService.StartBackgroundProgramSync();
                
                TempData["SuccessMessage"] = "✅ Program sync started in background. Check status below for progress.";
                _logger.LogInformation("✅ Program sync started in background");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to start program sync: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"❌ Failed to start program sync: {ex.Message}. Check application logs for details.";
            }

            return RedirectToAction(nameof(Sync));
        }

        [HttpGet]
        public async Task<IActionResult> GetSyncStatus(string syncType = "Programs")
        {
            var status = await _context.SyncStatuses
                .FirstOrDefaultAsync(s => s.SyncType == syncType);

            if (status == null)
            {
                return Json(new
                {
                    isRunning = false,
                    status = "Idle",
                    message = "No sync has been run yet.",
                    lastSyncTime = (DateTime?)null,
                    totalItems = 0,
                    processedItems = 0,
                    successCount = 0,
                    errorCount = 0,
                    skippedCount = 0,
                    progressPercent = 0
                });
            }

            // ✅ DEAD-TASK DETECTION: If status says running but it's been more than 30 minutes, mark as failed
            bool isActuallyRunning = status.IsRunning;
            if (status.IsRunning && status.StartedAt.HasValue)
            {
                var runningDuration = DateTime.UtcNow - status.StartedAt.Value;
                if (runningDuration.TotalMinutes >= 30) // 30-minute hard limit
                {
                    _logger.LogWarning($"⚠ DEAD-TASK DETECTED: Sync has been 'running' for {runningDuration.TotalMinutes:F1} minutes. Auto-resetting...");
                    
                    // Force reset the status
                    status.IsRunning = false;
                    status.CompletedAt = DateTime.UtcNow;
                    if (string.IsNullOrEmpty(status.LastMessage) || !status.LastMessage.Contains("timeout"))
                    {
                        status.LastMessage = $"⚠ Sync exceeded {30} minute timeout and was auto-reset";
                    }
                    // Ensure progress is at least what was processed
                    if (status.ProcessedItems < status.TotalItems && status.TotalItems > 0)
                    {
                        status.ProcessedItems = status.TotalItems; // Set to 100% on timeout
                    }
                    
                    await _context.SaveChangesAsync();
                    isActuallyRunning = false;
                }
            }

            // Determine status text
            string statusText;
            if (!isActuallyRunning)
            {
                if (status.CompletedAt.HasValue)
                {
                    // Check if it was an error
                    var lastMessage = status.LastMessage ?? "";
                    if (lastMessage.Contains("❌") || lastMessage.Contains("error") || lastMessage.Contains("failed") || lastMessage.Contains("timeout"))
                    {
                        statusText = "Failed";
                    }
                    else
                    {
                        statusText = "Completed";
                    }
                }
                else
                {
                    statusText = "Idle";
                }
            }
            else
            {
                statusText = "Running";
            }

            // Calculate progress - ensure it never exceeds 100%
            int progressPercent = 0;
            if (status.TotalItems > 0)
            {
                progressPercent = Math.Min(100, (int)((double)status.ProcessedItems / status.TotalItems * 100));
            }
            else if (!isActuallyRunning && status.CompletedAt.HasValue)
            {
                // If completed but no items, show 100%
                progressPercent = 100;
            }

            return Json(new
            {
                isRunning = isActuallyRunning,
                status = statusText,
                message = status.LastMessage ?? "No message available",
                lastSyncTime = status.CompletedAt ?? status.StartedAt,
                startedAt = status.StartedAt,
                totalItems = status.TotalItems,
                processedItems = status.ProcessedItems,
                successCount = status.SuccessCount,
                errorCount = status.ErrorCount,
                skippedCount = status.SkippedCount,
                progressPercent = progressPercent
            });
        }
    }
}
