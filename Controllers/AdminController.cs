using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityFinder.Models;
using UniversityFinder.Services;

namespace UniversityFinder.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly SupabaseService _supabaseService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            SupabaseService supabaseService,
            ILogger<AdminController> logger)
        {
            _supabaseService = supabaseService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Sync()
        {
            // TODO: Implement sync status tracking using Supabase
            // For now, return empty status
            ViewBag.ProgramsStatus = null;
            return View();
        }

        /// <summary>
        /// LEGACY: RVU import functionality removed
        /// Universities should be imported directly into Supabase
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncFromRvu()
        {
            TempData["ErrorMessage"] = "❌ RVU import service has been removed. Please import universities directly into Supabase.";
            _logger.LogWarning("⚠️ RVU sync requested but service is deprecated.");
            return RedirectToAction(nameof(Sync));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncUniversities()
        {
            // LEGACY: HEI API sync removed - replaced with RVU (NACID) data source
            // TODO: Implement RVU sync functionality
            TempData["ErrorMessage"] = "❌ HEI API sync is no longer available. Please use RVU (NACID) data source instead.";
            _logger.LogWarning("⚠ HEI university sync requested but service is deprecated. Use RVU (NACID) instead.");
            return RedirectToAction(nameof(Sync));
            
            /* LEGACY CODE - KEPT FOR REFERENCE
            try
            {
                _logger.LogInformation("🔄 HEI university sync requested by user {User}", User.Identity?.Name);
                var (inserted, fetched) = await _heiApiService.SyncUniversitiesAsync();
                TempData["SuccessMessage"] = $"✅ HEI sync complete. Fetched: {fetched}, Inserted: {inserted}.";
                _logger.LogInformation("✅ HEI sync completed successfully. Fetched: {FetchedCount}, Inserted: {InsertedCount}.", fetched, inserted);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "❌ University sync failed (HTTP error): {Message}", httpEx.Message);
                var errorMessage = httpEx.Message.Contains("401") 
                    ? "❌ University sync failed: 401 Unauthorized from Supabase. Check API key configuration and RLS policies."
                    : $"❌ University sync failed: {httpEx.Message}. Check application logs for details.";
                TempData["ErrorMessage"] = errorMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ University sync failed: {Message}", ex.Message);
                var errorMessage = ex.Message.Contains("401") || ex.Message.Contains("Unauthorized")
                    ? "❌ University sync failed: 401 Unauthorized from Supabase. Check API key configuration and RLS policies."
                    : $"❌ University sync failed: {ex.Message}. Check application logs for details.";
                TempData["ErrorMessage"] = errorMessage;
            }

            return RedirectToAction(nameof(Sync));
            */
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncPrograms()
        {
            // LEGACY: HEI API program sync removed - replaced with RVU (NACID) data source
            // TODO: Implement RVU program sync functionality
            TempData["ErrorMessage"] = "❌ HEI API program sync is no longer available. Please use RVU (NACID) data source instead.";
            _logger.LogWarning("⚠ HEI program sync requested but service is deprecated. Use RVU (NACID) instead.");
            return RedirectToAction(nameof(Sync));
            
            /* LEGACY CODE - KEPT FOR REFERENCE
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
            */
        }

        [HttpGet]
        public async Task<IActionResult> GetSyncStatus(string syncType = "Programs")
        {
            // LEGACY: EF Core removed - sync status tracking now uses Supabase
            // TODO: Implement sync status tracking using SupabaseService
            // For now, return idle status
            return Json(new
            {
                isRunning = false,
                status = "Idle",
                message = "Sync status tracking not yet implemented with Supabase.",
                lastSyncTime = (DateTime?)null,
                totalItems = 0,
                processedItems = 0,
                successCount = 0,
                errorCount = 0,
                skippedCount = 0,
                progressPercent = 0
            });
            
            /* LEGACY CODE - KEPT FOR REFERENCE
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
            */
        }

        /// <summary>
        /// Diagnostic endpoint to check university count in Supabase
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DebugUniversityCount()
        {
            try
            {
                // ✅ SUPABASE REST API: Get university count from Supabase via REST
                var universities = await _supabaseService.GetUniversitiesAsync();
                var totalCount = universities.Count;
                // Schema simplified - no HeiApiId field

                var result = new
                {
                    totalUniversities = totalCount,
                    timestamp = DateTime.UtcNow,
                    message = totalCount == 0 
                        ? "⚠️ WARNING: Universities table is EMPTY in Supabase. HEI sync may have failed." 
                        : $"✅ Found {totalCount} universities in Supabase (via REST API)"
                };

                _logger.LogInformation($"🔍 Debug: University count from Supabase REST = {totalCount}");

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting university count: {Message}", ex.Message);
                return Json(new
                {
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Debug endpoint to test Supabase REST API connection
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DebugSupabase()
        {
            try
            {
                var universities = await _supabaseService.GetUniversitiesAsync();
                return Content($"Supabase OK. Universities count: {universities.Count}");
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "DebugSupabase failed (HTTP error): {Message}", httpEx.Message);
                var errorMsg = httpEx.Message.Contains("401") 
                    ? "DebugSupabase failed: 401 Unauthorized - Check Supabase API key and RLS policies"
                    : $"DebugSupabase failed: {httpEx.Message}";
                return Content(errorMsg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DebugSupabase failed: {Message}", ex.Message);
                return Content("DebugSupabase failed: " + ex.Message);
            }
        }

        // ===================== CRUD OPERATIONS =====================

        /// <summary>
        /// Display list of universities for management
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var universities = await _supabaseService.GetUniversitiesAsync();
            return View(universities);
        }

        /// <summary>
        /// Display form to create a new university
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            return View(new University());
        }

        /// <summary>
        /// Create a new university
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(University university)
        {
            if (!ModelState.IsValid)
            {
                return View(university);
            }

            if (string.IsNullOrWhiteSpace(university.Name))
            {
                ModelState.AddModelError("Name", "University name is required.");
                return View(university);
            }

            try
            {
                var existing = await _supabaseService.GetUniversityByNameAsync(university.Name);
                if (existing != null)
                {
                    ModelState.AddModelError("Name", "A university with this name already exists.");
                    return View(university);
                }

                await _supabaseService.InsertUniversityAsync(university);
                TempData["SuccessMessage"] = $"✅ University '{university.Name}' created successfully.";
                _logger.LogInformation("✅ Admin {User} created university: {Name}", User.Identity?.Name, university.Name);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating university: {Message}", ex.Message);
                ModelState.AddModelError("", $"Error creating university: {ex.Message}");
                return View(university);
            }
        }

        /// <summary>
        /// Display form to edit a university
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var university = await _supabaseService.GetUniversityByIdAsync(id);
            if (university == null)
            {
                _logger.LogWarning("University not found: {Id}", id);
                TempData["ErrorMessage"] = "University not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(university);
        }

        /// <summary>
        /// Update an existing university
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, University university)
        {
            if (!ModelState.IsValid)
            {
                return View(university);
            }

            if (string.IsNullOrWhiteSpace(university.Name))
            {
                ModelState.AddModelError("Name", "University name is required.");
                return View(university);
            }

            try
            {
                var existing = await _supabaseService.GetUniversityByIdAsync(id);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "University not found.";
                    return RedirectToAction(nameof(Index));
                }

                // If name changed, check if new name already exists
                if (existing.Name != university.Name)
                {
                    var nameExists = await _supabaseService.GetUniversityByNameAsync(university.Name);
                    if (nameExists != null && nameExists.Id != id)
                    {
                        ModelState.AddModelError("Name", "A university with this name already exists.");
                        return View(university);
                    }
                }

                // Update using Id (primary key)
                var updated = await _supabaseService.UpdateUniversityAsync(id, university);
                
                if (updated == null)
                {
                    ModelState.AddModelError("", "Error updating university.");
                    return View(university);
                }

                TempData["SuccessMessage"] = $"✅ University '{university.Name}' updated successfully.";
                _logger.LogInformation("✅ Admin {User} updated university: {Id} - {Name}", User.Identity?.Name, id, university.Name);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating university: {Message}", ex.Message);
                ModelState.AddModelError("", $"Error updating university: {ex.Message}");
                return View(university);
            }
        }

        /// <summary>
        /// Display confirmation page to delete a university
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var university = await _supabaseService.GetUniversityByIdAsync(id);
            if (university == null)
            {
                TempData["ErrorMessage"] = "University not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(university);
        }

        /// <summary>
        /// Delete a university
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var university = await _supabaseService.GetUniversityByIdAsync(id);
                if (university == null)
                {
                    TempData["ErrorMessage"] = "University not found.";
                    return RedirectToAction(nameof(Index));
                }

                var deleted = await _supabaseService.DeleteUniversityAsync(id);
                
                if (!deleted)
                {
                    TempData["ErrorMessage"] = "Error deleting university.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["SuccessMessage"] = $"✅ University '{university.Name}' deleted successfully.";
                _logger.LogInformation("✅ Admin {User} deleted university: {Id} - {Name}", User.Identity?.Name, id, university.Name);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting university: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"Error deleting university: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
