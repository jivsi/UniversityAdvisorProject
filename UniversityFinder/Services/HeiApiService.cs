using System.Text.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UniversityFinder.Data;
using UniversityFinder.DTOs;
using UniversityFinder.Models;
using System.Threading;

namespace UniversityFinder.Services
{
    public class HeiApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HeiApiService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ISubjectInferenceService? _inferenceService;

        private const string BaseUrl = "https://hei.api.uni-foundation.eu";
        private const int BatchSize = 50;
        private const int ApiCallDelayMs = 300;
        private const int BatchDelayMs = 2000;
        private const int SyncTimeoutMinutes = 30; // Auto-reset if sync exceeds 30 minutes
        private const int MaxRetries = 3;
        private const int RetryBaseDelayMs = 1000;
        private const int DbBatchSize = 100; // Batch size for database operations

        public HeiApiService(
            HttpClient httpClient,
            ApplicationDbContext context,
            ILogger<HeiApiService> logger,
            IServiceProvider serviceProvider,
            ISubjectInferenceService? inferenceService = null)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _inferenceService = inferenceService;

            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<int> SyncUniversitiesAsync(CancellationToken cancellationToken = default)
        {
            int totalInserted = 0;
            int totalSkipped = 0;

            _logger.LogInformation("🚀 Starting HEI University Sync...");

            var countries = await _context.Countries
                .Where(c => !string.IsNullOrWhiteSpace(c.Code))
                .ToListAsync(cancellationToken);

            _logger.LogInformation($"📋 Found {countries.Count} countries with ISO codes");

            if (countries.Count == 0)
            {
                _logger.LogWarning("⚠ No countries found in database. Please seed countries first.");
                return 0;
            }

            // Pre-load all existing HeiApiIds for efficient duplicate checking
            var existingHeiApiIdsList = await _context.Universities
                .Where(u => !string.IsNullOrWhiteSpace(u.HeiApiId))
                .Select(u => u.HeiApiId!)
                .ToListAsync(cancellationToken);
            var existingHeiApiIds = existingHeiApiIdsList.ToHashSet();

            _logger.LogInformation($"📊 Found {existingHeiApiIds.Count} existing universities in database");

            foreach (var country in countries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var isoCode = country.Code!.Trim().ToUpper();
                    _logger.LogInformation($"➡ Processing {country.Name} (ISO: {isoCode})");

                    var apiUniversities = await FetchUniversitiesByCountryWithRetryAsync(isoCode, cancellationToken);

                    if (apiUniversities == null || apiUniversities.Count == 0)
                    {
                        _logger.LogInformation($"ℹ No universities returned for {country.Name}");
                        await Task.Delay(300, cancellationToken);
                        continue;
                    }

                    _logger.LogInformation($"📥 Received {apiUniversities.Count} universities for {country.Name}");

                    int countryInserted = 0;
                    int countrySkipped = 0;
                    var universitiesToAdd = new List<University>();
                    var citiesToAdd = new Dictionary<string, City>(); // Key: cityName_countryId
                    var citiesToCheck = new HashSet<string>(); // Track which cities we need to check

                    // Pre-load existing cities for this country
                    var existingCities = await _context.Cities
                        .Where(c => c.CountryId == country.Id)
                        .ToDictionaryAsync(c => c.Name, c => c, cancellationToken);

                    foreach (var apiUni in apiUniversities)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (string.IsNullOrWhiteSpace(apiUni.Id))
                            {
                                _logger.LogWarning($"⚠ Skipping university with empty ID for country {country.Name}");
                                countrySkipped++;
                                continue;
                            }

                            // Fast duplicate check using HashSet
                            if (existingHeiApiIds.Contains(apiUni.Id))
                            {
                                _logger.LogDebug($"⏭ Skipping university {apiUni.Id} - already exists in database");
                                countrySkipped++;
                                continue;
                            }

                            var universityName = apiUni.Attributes?.FirstName?.Trim();
                            if (string.IsNullOrWhiteSpace(universityName))
                            {
                                universityName = apiUni.Attributes?.HeiId?.Trim();
                                if (string.IsNullOrWhiteSpace(universityName))
                                {
                                    universityName = apiUni.Id;
                                }
                                _logger.LogWarning($"⚠ University {apiUni.Id} missing name, using: {universityName}");
                            }

                            var cityName = apiUni.Attributes?.City?.Trim();
                            if (string.IsNullOrWhiteSpace(cityName))
                            {
                                cityName = "Unknown";
                            }

                            // Get or create city (batch operation)
                            City city;
                            var cityKey = $"{cityName}_{country.Id}";
                            
                            if (existingCities.TryGetValue(cityName, out var existingCity))
                            {
                                city = existingCity;
                            }
                            else if (citiesToAdd.TryGetValue(cityKey, out var queuedCity))
                            {
                                city = queuedCity;
                            }
                            else
                            {
                                city = new City
                                {
                                    Name = cityName,
                                    CountryId = country.Id
                                };
                                citiesToAdd[cityKey] = city;
                                _context.Cities.Add(city);
                                existingCities[cityName] = city; // Cache for subsequent universities
                                _logger.LogDebug($"➕ Queued city: {cityName}");
                            }

                            var university = new University
                            {
                                Name = universityName,
                                Acronym = apiUni.Attributes?.Acronym?.Trim(),
                                Website = apiUni.Attributes?.WebsiteUrl?.Trim(),
                                Description = null,
                                CountryId = country.Id,
                                CityId = city.Id, // Will be set after SaveChanges
                                HeiApiId = apiUni.Id,
                                EstablishedYear = null
                            };

                            universitiesToAdd.Add(university);
                            existingHeiApiIds.Add(apiUni.Id); // Track in memory to avoid duplicates in same batch
                            countryInserted++;

                            _logger.LogDebug($"➕ Queued: {universityName} ({apiUni.Id})");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"❌ Error processing university {apiUni.Id}: {ex.Message}");
                            countrySkipped++;
                        }
                    }

                    // Batch save: cities first, then universities
                    if (citiesToAdd.Count > 0)
                    {
                        try
                        {
                            await _context.SaveChangesAsync(cancellationToken);
                            _logger.LogInformation($"💾 Saved {citiesToAdd.Count} new cities for {country.Name}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"❌ Database save failed for cities in {country.Name}: {ex.Message}");
                            // Continue - some cities might already exist
                        }
                    }

                    if (universitiesToAdd.Count > 0)
                    {
                        try
                        {
                            // Set CityId now that cities are saved
                            foreach (var uni in universitiesToAdd)
                            {
                                if (uni.CityId == 0 && existingCities.TryGetValue(uni.Name, out var city))
                                {
                                    uni.CityId = city.Id;
                                }
                            }

                            await _context.SaveChangesAsync(cancellationToken);
                            _logger.LogInformation($"💾 Saved {countryInserted} new universities for {country.Name} ({countrySkipped} skipped: already existed or invalid)");
                            totalInserted += countryInserted;
                            totalSkipped += countrySkipped;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"❌ Database save failed for universities in {country.Name}: {ex.Message}");
                            _logger.LogError($"   Stack trace: {ex.StackTrace}");
                            // Continue with next country instead of throwing
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"ℹ No new universities for {country.Name} ({countrySkipped} skipped: already existed or invalid)");
                        totalSkipped += countrySkipped;
                    }

                    await Task.Delay(600, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("⚠ University sync cancelled by user");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error processing country {country.Name}: {ex.Message}");
                }
            }

            _logger.LogInformation($"✅ Sync complete! Total universities added: {totalInserted}, Total skipped: {totalSkipped}");
            return totalInserted;
        }

        /// <summary>
        /// Starts a background sync of programs for all universities
        /// Uses fire-and-forget pattern with GUARANTEED cleanup and status update
        /// </summary>
        public void StartBackgroundProgramSync()
        {
            // Use Task.Run to avoid blocking the HTTP request
            // CRITICAL: Use ContinueWith to ensure status is ALWAYS updated, even if task fails
            _ = Task.Run(async () =>
            {
                // Create a separate scope for the background task to avoid context disposal issues
                using var scope = _serviceProvider.CreateScope();
                ApplicationDbContext? context = null;
                ILogger<HeiApiService>? logger = null;
                ISubjectInferenceService? inferenceService = null;
                bool syncCompleted = false;
                string? finalError = null;

                try
                {
                    context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    logger = scope.ServiceProvider.GetRequiredService<ILogger<HeiApiService>>();
                    inferenceService = scope.ServiceProvider.GetService<ISubjectInferenceService>();

                    if (context == null || logger == null)
                    {
                        _logger.LogError("❌ Failed to resolve required services for background sync");
                        finalError = "Failed to resolve required services";
                        return;
                    }

                    logger.LogInformation("🚀 Background sync task started");
                    await SyncProgramsBackgroundAsync(context, logger, inferenceService);
                    syncCompleted = true;
                    logger.LogInformation("✅ Background sync task completed successfully");
                }
                catch (Exception ex)
                {
                    // Last resort error handling - ensure we log even if context is null
                    var errorLogger = logger ?? _logger;
                    finalError = $"Fatal error: {ex.Message}";
                    errorLogger.LogError(ex, "❌ Fatal error in StartBackgroundProgramSync: {Message}", ex.Message);
                    
                    // Try to reset sync status if context is available
                    if (context != null)
                    {
                        try
                        {
                            await ResetStaleSyncStatusAsync(context, "Programs", errorLogger);
                        }
                        catch (Exception resetEx)
                        {
                            errorLogger.LogError(resetEx, "❌ Failed to reset stale sync status: {Message}", resetEx.Message);
                        }
                    }
                }
                finally
                {
                    // ✅ GUARANTEED CLEANUP: Always ensure status is updated, even if everything else fails
                    // Use a fresh scope to guarantee we can update status
                    if (!syncCompleted || !string.IsNullOrEmpty(finalError))
                    {
                        try
                        {
                            using var cleanupScope = _serviceProvider.CreateScope();
                            var cleanupContext = cleanupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                            var cleanupLogger = cleanupScope.ServiceProvider.GetRequiredService<ILogger<HeiApiService>>();
                            
                            var status = await cleanupContext.SyncStatuses
                                .FirstOrDefaultAsync(s => s.SyncType == "Programs");
                            
                            if (status != null && status.IsRunning)
                            {
                                status.IsRunning = false;
                                status.CompletedAt = DateTime.UtcNow;
                                if (!string.IsNullOrEmpty(finalError))
                                {
                                    status.LastMessage = $"❌ Sync failed: {finalError}";
                                }
                                else if (string.IsNullOrEmpty(status.LastMessage))
                                {
                                    status.LastMessage = "⚠ Sync was terminated unexpectedly";
                                }
                                
                                await cleanupContext.SaveChangesAsync();
                                cleanupLogger.LogInformation("✅ GUARANTEED: Sync status reset in outer finally block");
                            }
                        }
                        catch (Exception finalEx)
                        {
                            // Last resort - log but don't throw
                            var finalLogger = logger ?? _logger;
                            finalLogger.LogError(finalEx, "❌ CRITICAL: Even guaranteed cleanup failed: {Message}", finalEx.Message);
                        }
                    }
                }
            }).ContinueWith(task =>
            {
                // ✅ ADDITIONAL SAFETY: If task itself fails, ensure status is updated
                if (task.IsFaulted && task.Exception != null)
                {
                    try
                    {
                        using var emergencyScope = _serviceProvider.CreateScope();
                        var emergencyContext = emergencyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var emergencyLogger = emergencyScope.ServiceProvider.GetRequiredService<ILogger<HeiApiService>>();
                        
                        var status = emergencyContext.SyncStatuses
                            .FirstOrDefault(s => s.SyncType == "Programs" && s.IsRunning);
                        
                        if (status != null)
                        {
                            status.IsRunning = false;
                            status.CompletedAt = DateTime.UtcNow;
                            status.LastMessage = $"❌ Task faulted: {task.Exception.InnerException?.Message ?? "Unknown error"}";
                            emergencyContext.SaveChanges();
                            emergencyLogger.LogError(task.Exception, "❌ EMERGENCY: Task faulted, status reset");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ CRITICAL: Emergency cleanup also failed: {Message}", ex.Message);
                    }
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Background sync of programs with batching, rate limiting, and progress tracking
        /// HARDENED: Guaranteed cleanup with try/finally, timeout protection, and stale state detection
        /// </summary>
        private async Task SyncProgramsBackgroundAsync(ApplicationDbContext context, ILogger<HeiApiService> logger, ISubjectInferenceService? inferenceService = null)
        {
            const string syncType = "Programs";
            SyncStatus? status = null;
            var syncStartedAt = DateTime.UtcNow;
            string? errorMessage = null; // ✅ Scope: Declared outside try/finally for access in finally block
            
            // ✅ TIMEOUT PROTECTION: Create cancellation token with 30-minute timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(SyncTimeoutMinutes));
            var cancellationToken = timeoutCts.Token;
            
            // ✅ HARD TIMEOUT: Start a watchdog task that will force status update if sync exceeds timeout
            using var watchdogCts = new CancellationTokenSource();
            var watchdogTask = Task.Run(async () =>
            {
                try
                {
                    // Wait for timeout + 1 minute buffer
                    await Task.Delay(TimeSpan.FromMinutes(SyncTimeoutMinutes + 1), watchdogCts.Token);
                    
                    // If we reach here (not cancelled), sync has exceeded timeout - force status update
                    logger.LogWarning("⚠ HARD TIMEOUT: Sync exceeded {Timeout} minutes, forcing status update", SyncTimeoutMinutes);
                    
                    using var emergencyScope = _serviceProvider.CreateScope();
                    var emergencyContext = emergencyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var emergencyLogger = emergencyScope.ServiceProvider.GetRequiredService<ILogger<HeiApiService>>();
                    
                    var staleStatus = await emergencyContext.SyncStatuses
                        .FirstOrDefaultAsync(s => s.SyncType == syncType && s.IsRunning);
                    
                    if (staleStatus != null)
                    {
                        staleStatus.IsRunning = false;
                        staleStatus.CompletedAt = DateTime.UtcNow;
                        staleStatus.LastMessage = $"⚠ Sync exceeded {SyncTimeoutMinutes} minute timeout and was forcibly terminated";
                        // Ensure progress shows completion
                        if (staleStatus.ProcessedItems < staleStatus.TotalItems && staleStatus.TotalItems > 0)
                        {
                            staleStatus.ProcessedItems = staleStatus.TotalItems;
                        }
                        await emergencyContext.SaveChangesAsync();
                        emergencyLogger.LogWarning("✅ HARD TIMEOUT: Forced status update completed - IsRunning=false, Progress=100%");
                    }
                }
                catch (OperationCanceledException)
                {
                    // Watchdog was cancelled because sync completed - this is expected
                    logger.LogDebug("✅ Watchdog cancelled - sync completed normally");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ HARD TIMEOUT watchdog failed: {Message}", ex.Message);
                }
            }, watchdogCts.Token);

            try
            {
                // ✅ STALE STATE CHECK: Reset any stale running states before starting
                await ResetStaleSyncStatusIfNeededAsync(context, syncType, logger);

                // ✅ CONCURRENT SYNC PROTECTION: Check if sync is already running with database-level check
                status = await context.SyncStatuses
                    .FirstOrDefaultAsync(s => s.SyncType == syncType, cancellationToken);

                if (status != null && status.IsRunning)
                {
                    // Double-check: verify it's not a stale state
                    if (status.StartedAt.HasValue && 
                        (DateTime.UtcNow - status.StartedAt.Value).TotalMinutes < SyncTimeoutMinutes)
                    {
                        logger.LogWarning("⚠ Program sync is already running. Skipping new sync request.");
                        return;
                    }
                    else
                    {
                        // Stale running state detected - reset it
                        logger.LogWarning($"⚠ Detected stale running state (started at {status.StartedAt}). Resetting...");
                        await ResetStaleSyncStatusAsync(context, syncType, logger);
                        status = null;
                    }
                }

                // Create or update sync status
                if (status == null)
                {
                    status = new SyncStatus
                    {
                        SyncType = syncType,
                        IsRunning = true,
                        StartedAt = syncStartedAt,
                        LastMessage = "Starting program sync...",
                        TotalItems = 0,
                        ProcessedItems = 0,
                        SuccessCount = 0,
                        ErrorCount = 0,
                        SkippedCount = 0
                    };
                    context.SyncStatuses.Add(status);
                }
                else
                {
                    status.IsRunning = true;
                    status.StartedAt = syncStartedAt;
                    status.CompletedAt = null;
                    status.LastMessage = "Starting program sync...";
                    status.ProcessedItems = 0;
                    status.SuccessCount = 0;
                    status.ErrorCount = 0;
                    status.SkippedCount = 0;
                }

                await context.SaveChangesAsync(cancellationToken);

                // Get all universities with HeiApiId
                var universities = await context.Universities
                    .Where(u => !string.IsNullOrWhiteSpace(u.HeiApiId))
                    .OrderBy(u => u.Id)
                    .ToListAsync();

                status.TotalItems = universities.Count;
                status.LastMessage = $"Found {universities.Count} universities to sync. Processing in batches of {BatchSize}...";
                await context.SaveChangesAsync();

                logger.LogInformation("🚀 Starting HEI Program Sync (Background)");
                logger.LogInformation($"📋 Total universities: {universities.Count}");
                logger.LogInformation($"⏱ Sync timeout set to {SyncTimeoutMinutes} minutes. Watchdog will force completion if exceeded.");

                int totalProgramsSynced = 0;
                int batchNumber = 0;
                var batches = universities.Chunk(BatchSize);

                // Process universities with timeout protection
                foreach (var batch in batches)
                {
                    // Check for timeout cancellation
                    cancellationToken.ThrowIfCancellationRequested();

                    batchNumber++;
                    logger.LogInformation($"📦 Processing batch {batchNumber} ({batch.Count()} universities)");

                    foreach (var university in batch)
                    {
                        // Check for timeout cancellation before each university
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            status.ProcessedItems++;
                            var progressPercent = status.TotalItems > 0 
                                ? (int)((double)status.ProcessedItems / status.TotalItems * 100) 
                                : 0;
                            status.LastMessage = $"Processing university {status.ProcessedItems}/{status.TotalItems} ({progressPercent}%): {university.Name}";
                            
                            // Update progress every 10 items or at batch boundaries to reduce DB writes
                            if (status.ProcessedItems % 10 == 0 || status.ProcessedItems == status.TotalItems)
                            {
                                await context.SaveChangesAsync(cancellationToken);
                            }

                            logger.LogInformation($"🔄 [{status.ProcessedItems}/{status.TotalItems}] Processing: {university.Name}");

                            // Always attempt to sync - let the method handle duplicate detection per program
                            var syncResult = await SyncProgramsForUniversityAsync(university, context, logger, inferenceService, cancellationToken);
                            
                            if (syncResult.ProgramsAdded > 0)
                            {
                                totalProgramsSynced += syncResult.ProgramsAdded;
                                status.SuccessCount++;
                                logger.LogInformation($"✅ {university.Name}: {syncResult.ProgramsAdded} programs added, {syncResult.ProgramsSkipped} already existed");
                            }
                            else if (syncResult.ProgramsSkipped > 0)
                            {
                                // API returned programs but all were duplicates
                                status.SkippedCount++;
                                logger.LogInformation($"⏭ {university.Name}: All {syncResult.ProgramsSkipped} programs already exist (skipped - reason: duplicates)");
                            }
                            else if (syncResult.ApiProgramsCount == 0)
                            {
                                // API returned no programs
                                status.SkippedCount++;
                                logger.LogInformation($"ℹ {university.Name}: No programs found in HEI API (skipped - reason: no API data)");
                            }
                            else
                            {
                                // Edge case: API returned programs but none could be processed
                                status.SkippedCount++;
                                logger.LogWarning($"⚠ {university.Name}: {syncResult.ApiProgramsCount} programs returned but none could be processed (skipped - reason: processing error)");
                            }

                            // Rate limiting between API calls
                            await Task.Delay(ApiCallDelayMs, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // Timeout or cancellation - will be handled in outer catch
                            throw;
                        }
                        catch (Exception ex)
                        {
                            status.ErrorCount++;
                            var errorReason = ex is HttpRequestException ? "API error" : 
                                            ex is TaskCanceledException ? "Timeout" : 
                                            ex is OperationCanceledException ? "Cancelled" : "Unknown error";
                            logger.LogError(ex, $"❌ Error syncing programs for university {university.Id} ({university.Name}): {ex.Message} (Reason: {errorReason})");
                            status.LastMessage = $"Error processing {university.Name}: {errorReason}";
                            
                            // Update status periodically, not on every error to reduce DB writes
                            if (status.ErrorCount % 10 == 0)
                            {
                                try
                                {
                                    await context.SaveChangesAsync(cancellationToken);
                                }
                                catch (Exception saveEx)
                                {
                                    logger.LogError(saveEx, $"❌ Failed to save status after error: {saveEx.Message}");
                                }
                            }
                            
                            // Continue with next university - don't stop the sync
                        }
                    }

                    // Delay between batches (with cancellation support)
                    if (batchNumber < batches.Count())
                    {
                        logger.LogInformation($"⏸ Waiting {BatchDelayMs}ms before next batch...");
                        await Task.Delay(BatchDelayMs, cancellationToken);
                    }
                }

                // ✅ SUCCESS: Ensure progress is 100% and final status is set
                if (status != null)
                {
                    // CRITICAL: Ensure processedItems equals totalItems for 100% progress
                    status.ProcessedItems = status.TotalItems;
                    status.LastMessage = $"✅ Sync complete! {totalProgramsSynced} programs synced. Success: {status.SuccessCount}, Errors: {status.ErrorCount}, Skipped: {status.SkippedCount}";
                    
                    // Force final status update before finally block
                    try
                    {
                        await context.SaveChangesAsync(cancellationToken);
                        logger.LogInformation("✅ Final status update saved: Progress 100%, IsRunning will be set to false in finally");
                    }
                    catch (Exception saveEx)
                    {
                        logger.LogError(saveEx, "⚠ Failed to save final status, will retry in finally block");
                    }
                }

                logger.LogInformation($"✅ Program sync complete! Total programs synced: {totalProgramsSynced}");
                logger.LogInformation($"📊 Summary - Success: {status?.SuccessCount ?? 0}, Errors: {status?.ErrorCount ?? 0}, Skipped: {status?.SkippedCount ?? 0}");
                
                // Cancel watchdog task since we completed successfully
                watchdogCts.Cancel();
                logger.LogDebug("✅ Watchdog task cancelled - sync completed successfully");
            }
            catch (OperationCanceledException)
            {
                errorMessage = $"❌ Sync timeout after {SyncTimeoutMinutes} minutes. Auto-reset.";
                logger.LogWarning($"⚠ {errorMessage}");
                watchdogCts.Cancel(); // Cancel watchdog since we're handling timeout
            }
            catch (Exception ex)
            {
                errorMessage = $"❌ Fatal error in background program sync: {ex.Message}";
                logger.LogError(ex, errorMessage);
                watchdogCts.Cancel(); // Cancel watchdog since we're handling error
            }
            finally
            {
                // ✅ GUARANTEED CLEANUP: Always reset IsRunning flag, even if exception occurs
                // Use multiple fallback mechanisms to ensure status is ALWAYS updated
                bool statusUpdated = false;
                
                // Attempt 1: Use existing context (fastest)
                if (status != null && !statusUpdated)
                {
                    try
                    {
                        // Re-fetch status to get latest state (in case context was disposed)
                        var refreshedStatus = await context.SyncStatuses
                            .FirstOrDefaultAsync(s => s.SyncType == syncType);
                        
                        if (refreshedStatus != null && refreshedStatus.IsRunning)
                        {
                            refreshedStatus.IsRunning = false;
                            refreshedStatus.CompletedAt = DateTime.UtcNow;
                            
                            // Ensure progress is 100%
                            if (refreshedStatus.ProcessedItems < refreshedStatus.TotalItems && refreshedStatus.TotalItems > 0)
                            {
                                refreshedStatus.ProcessedItems = refreshedStatus.TotalItems;
                            }
                            
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                refreshedStatus.LastMessage = errorMessage;
                            }
                            else if (string.IsNullOrEmpty(refreshedStatus.LastMessage) || 
                                     refreshedStatus.LastMessage == "Starting program sync...")
                            {
                                refreshedStatus.LastMessage = "✅ Sync finished";
                            }

                            await context.SaveChangesAsync();
                            statusUpdated = true;
                            logger.LogInformation("✅ GUARANTEED CLEANUP (Attempt 1): Sync status reset successfully - IsRunning=false, Progress=100%");
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        logger.LogWarning(cleanupEx, "⚠ Attempt 1 failed, trying fallback: {Message}", cleanupEx.Message);
                    }
                }
                
                // Attempt 2: Use fresh context (if attempt 1 failed or context was disposed)
                if (!statusUpdated)
                {
                    try
                    {
                        using var cleanupScope = _serviceProvider.CreateScope();
                        var cleanupContext = cleanupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var cleanupLogger = cleanupScope.ServiceProvider.GetRequiredService<ILogger<HeiApiService>>();
                        
                        var refreshedStatus = await cleanupContext.SyncStatuses
                            .FirstOrDefaultAsync(s => s.SyncType == syncType);
                        
                        if (refreshedStatus != null && refreshedStatus.IsRunning)
                        {
                            refreshedStatus.IsRunning = false;
                            refreshedStatus.CompletedAt = DateTime.UtcNow;
                            
                            // Ensure progress is 100%
                            if (refreshedStatus.ProcessedItems < refreshedStatus.TotalItems && refreshedStatus.TotalItems > 0)
                            {
                                refreshedStatus.ProcessedItems = refreshedStatus.TotalItems;
                            }
                            
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                refreshedStatus.LastMessage = errorMessage;
                            }
                            else if (string.IsNullOrEmpty(refreshedStatus.LastMessage))
                            {
                                refreshedStatus.LastMessage = "✅ Sync finished";
                            }
                            
                            await cleanupContext.SaveChangesAsync();
                            statusUpdated = true;
                            cleanupLogger.LogInformation("✅ GUARANTEED CLEANUP (Attempt 2): Sync status reset with fresh context - IsRunning=false, Progress=100%");
                        }
                    }
                    catch (Exception cleanupEx2)
                    {
                        logger.LogError(cleanupEx2, "❌ CRITICAL: Attempt 2 also failed: {Message}", cleanupEx2.Message);
                    }
                }
                
                // Attempt 3: Direct database update (last resort)
                if (!statusUpdated)
                {
                    try
                    {
                        using var emergencyScope = _serviceProvider.CreateScope();
                        var emergencyContext = emergencyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var emergencyLogger = emergencyScope.ServiceProvider.GetRequiredService<ILogger<HeiApiService>>();
                        
                        // Use raw SQL as absolute last resort
                        await emergencyContext.Database.ExecuteSqlRawAsync(
                            "UPDATE SyncStatuses SET IsRunning = 0, CompletedAt = datetime('now'), ProcessedItems = TotalItems WHERE SyncType = 'Programs' AND IsRunning = 1");
                        
                        statusUpdated = true;
                        emergencyLogger.LogWarning("✅ GUARANTEED CLEANUP (Attempt 3): Sync status reset using raw SQL - IsRunning=false");
                    }
                    catch (Exception finalEx)
                    {
                        logger.LogError(finalEx, "❌ CRITICAL: All cleanup attempts failed. Status may remain stuck. Manual intervention required.");
                    }
                }
                
                if (statusUpdated)
                {
                    logger.LogInformation("✅ GUARANTEED CLEANUP: Status update completed successfully via one of the fallback mechanisms");
                }
                
                // Cancel watchdog task if it's still running
                try
                {
                    watchdogCts.Cancel();
                }
                catch { /* Ignore if already cancelled */ }
                
                logger.LogInformation("✅ FINALIZATION: Finally block completed - status should be updated");
            }
        }

        /// <summary>
        /// Syncs programs for all universities that have a HeiApiId (legacy synchronous method)
        /// </summary>
        public async Task<int> SyncProgramsAsync()
        {
            int totalProgramsSynced = 0;

            _logger.LogInformation("🚀 Starting HEI Program Sync...");

            var universities = await _context.Universities
                .Where(u => !string.IsNullOrWhiteSpace(u.HeiApiId))
                .ToListAsync();

            _logger.LogInformation($"📋 Found {universities.Count} universities to sync programs for");

            foreach (var university in universities)
            {
                try
                {
                    var programsSynced = await SyncProgramsForUniversityAsync(university);
                    totalProgramsSynced += programsSynced;

                    // Rate limiting - be respectful to the API
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"❌ Error syncing programs for university {university.Id} ({university.Name}): {ex.Message}");
                }
            }

            _logger.LogInformation($"✅ Program sync complete! Total programs synced: {totalProgramsSynced}");
            return totalProgramsSynced;
        }

        /// <summary>
        /// Syncs programs for a specific university (uses default context)
        /// Legacy method for backward compatibility
        /// </summary>
        private async Task<int> SyncProgramsForUniversityAsync(University university)
        {
            var result = await SyncProgramsForUniversityAsync(university, _context, _logger, _inferenceService);
            return result.ProgramsAdded;
        }

        /// <summary>
        /// Result of syncing programs for a university
        /// </summary>
        private class SyncProgramsResult
        {
            public int ProgramsAdded { get; set; }
            public int ProgramsSkipped { get; set; }
            public int ApiProgramsCount { get; set; }
        }

        /// <summary>
        /// Syncs programs for a specific university (with context parameter for background sync)
        /// Returns detailed result including counts
        /// </summary>
        private async Task<SyncProgramsResult> SyncProgramsForUniversityAsync(University university, ApplicationDbContext context, ILogger<HeiApiService> logger, ISubjectInferenceService? inferenceService = null, CancellationToken cancellationToken = default)
        {
            var result = new SyncProgramsResult();

            if (string.IsNullOrWhiteSpace(university.HeiApiId))
            {
                logger.LogWarning($"⚠ University {university.Id} ({university.Name}) has no HeiApiId, skipping");
                return result;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Always fetch from API - don't skip based on existing programs
                var apiPrograms = await FetchProgramsByHeiIdWithRetryAsync(university.HeiApiId, logger, cancellationToken);
                result.ApiProgramsCount = apiPrograms?.Count ?? 0;

                if (apiPrograms == null || apiPrograms.Count == 0)
                {
                    logger.LogInformation($"ℹ {university.Name}: No programs found in HEI API (HEI ID: {university.HeiApiId})");
                    
                    // ✅ FALLBACK: Run subject inference if HEI API returned no programs
                    // Use provided inferenceService or try to get from service provider
                    var inferenceServiceToUse = inferenceService ?? _inferenceService;
                    if (inferenceServiceToUse == null)
                    {
                        try
                        {
                            inferenceServiceToUse = _serviceProvider.GetService<ISubjectInferenceService>();
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, $"Could not get inference service from service provider: {ex.Message}");
                        }
                    }
                    
                    var inferenceResult = await RunSubjectInferenceFallbackAsync(university, context, logger, inferenceServiceToUse);
                    result.ProgramsAdded = inferenceResult.ProgramsAdded;
                    result.ProgramsSkipped = inferenceResult.ProgramsSkipped;
                    
                    return result;
                }

                logger.LogInformation($"📥 {university.Name}: HEI API returned {apiPrograms.Count} program(s)");

                // Pre-load existing programs for this university to avoid individual queries
                var existingProgramKeys = await context.Programs
                    .Where(p => p.UniversityId == university.Id)
                    .Select(p => new { p.Name, p.SubjectId })
                    .ToListAsync(cancellationToken);
                
                var existingProgramSet = existingProgramKeys
                    .Select(p => $"{p.Name}|{p.SubjectId}")
                    .ToHashSet();

                int programsAdded = 0;
                int programsSkipped = 0;
                int programsWithNoSubjects = 0;
                var programsToAdd = new List<UniversityProgram>();
                var subjectsToCreate = new Dictionary<string, Subject>(); // Key: normalized subject name
                var subjectAliasesToAdd = new Dictionary<int, List<(string Name, string Language)>>(); // SubjectId -> list of aliases

                foreach (var apiProgram in apiPrograms)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(apiProgram.Id))
                        {
                            logger.LogWarning($"⚠ Skipping program with empty ID for university {university.Name}");
                            continue;
                        }

                        // Get program name first
                        var programName = apiProgram.Attributes?.FirstName ?? apiProgram.Id;
                        if (string.IsNullOrWhiteSpace(programName))
                        {
                            programName = apiProgram.Id;
                        }

                        // Extract ALL subject area labels with their language codes
                        var subjectAreaLabels = apiProgram.Attributes?.AllSubjectAreaLabels ?? new List<DTOs.SubjectAreaLabel>();
                        
                        if (subjectAreaLabels.Count == 0)
                        {
                            // If no subject area, try to infer from program name
                            if (!string.IsNullOrWhiteSpace(programName))
                            {
                                subjectAreaLabels.Add(new DTOs.SubjectAreaLabel
                                {
                                    Name = programName,
                                    Language = "unknown"
                                });
                            }
                        }

                        if (subjectAreaLabels.Count == 0)
                        {
                            logger.LogDebug($"⚠ {university.Name}: Program '{programName}' has no subject areas, skipping");
                            programsWithNoSubjects++;
                            continue;
                        }

                        var degreeType = apiProgram.Attributes?.FirstQualification;
                        var duration = apiProgram.Attributes?.Duration?.Value;
                        var language = apiProgram.Attributes?.LanguageOfInstruction?.FirstOrDefault()?.FirstLabel;
                        var description = apiProgram.Attributes?.Description?.FirstOrDefault()?.String;

                        bool programAdded = false;

                        // Process each subject area - create subjects with all their aliases
                        foreach (var subjectLabel in subjectAreaLabels)
                        {
                            try
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                
                                // Get or create subject with all aliases (batched)
                                var subject = await GetOrCreateSubjectWithAliasesBatchedAsync(
                                    subjectLabel.Name, 
                                    subjectLabel.Language,
                                    context,
                                    logger,
                                    subjectsToCreate,
                                    subjectAliasesToAdd,
                                    subjectAreaLabels.Where(l => l != subjectLabel).ToList(),
                                    cancellationToken);

                                // Fast duplicate check using pre-loaded HashSet
                                var programKey = $"{programName}|{subject.Id}";
                                if (existingProgramSet.Contains(programKey))
                                {
                                    // This specific program-subject combination already exists
                                    continue;
                                }

                                var program = new UniversityProgram
                                {
                                    UniversityId = university.Id,
                                    SubjectId = subject.Id,
                                    Name = programName,
                                    DegreeType = degreeType,
                                    Duration = duration,
                                    Language = language,
                                    Description = description,
                                    IsInferred = false // Real HEI API program
                                };

                                programsToAdd.Add(program);
                                existingProgramSet.Add(programKey); // Track in memory
                                programsAdded++;
                                programAdded = true;
                            }
                            catch (OperationCanceledException)
                            {
                                throw;
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, $"❌ Error processing subject {subjectLabel.Name} for program {apiProgram.Id} ({university.Name}): {ex.Message}");
                            }
                        }

                        if (!programAdded)
                        {
                            // All subject-program combinations for this program already exist
                            programsSkipped++;
                            logger.LogDebug($"⏭ {university.Name}: Program '{programName}' already exists for all subjects");
                        }
                        else
                        {
                            logger.LogDebug($"➕ {university.Name}: Added program '{programName}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"❌ Error processing program {apiProgram.Id} for {university.Name}: {ex.Message}");
                    }
                }

                // Batch save: subjects first, then add aliases
                // Note: Aliases are handled separately after subjects are saved to avoid complexity
                // For now, we'll add aliases individually after subject creation (still batched per subject)
                if (subjectsToCreate.Count > 0)
                {
                    try
                    {
                        await context.SaveChangesAsync(cancellationToken);
                        logger.LogDebug($"💾 {university.Name}: Saved {subjectsToCreate.Count} new subject(s)");
                        
                        // Add aliases for newly created subjects (now they have IDs)
                        var allAliasesToAdd = new List<SubjectAlias>();
                        foreach (var subject in subjectsToCreate.Values)
                        {
                            if (subject.Id > 0 && subjectAliasesToAdd.TryGetValue(0, out var aliases))
                            {
                                foreach (var (aliasName, aliasLang) in aliases)
                                {
                                    // Check if alias already exists
                                    var aliasExists = await context.SubjectAliases
                                        .AnyAsync(a => a.SubjectId == subject.Id && 
                                                      a.Name == aliasName && 
                                                      a.LanguageCode == aliasLang, cancellationToken);
                                    if (!aliasExists)
                                    {
                                        allAliasesToAdd.Add(new SubjectAlias
                                        {
                                            SubjectId = subject.Id,
                                            Name = aliasName,
                                            LanguageCode = aliasLang
                                        });
                                    }
                                }
                            }
                        }
                        
                        // Process aliases for existing subjects
                        foreach (var kvp in subjectAliasesToAdd.Where(k => k.Key > 0))
                        {
                            foreach (var (aliasName, aliasLang) in kvp.Value)
                            {
                                var aliasExists = await context.SubjectAliases
                                    .AnyAsync(a => a.SubjectId == kvp.Key && 
                                                  a.Name == aliasName && 
                                                  a.LanguageCode == aliasLang, cancellationToken);
                                if (!aliasExists)
                                {
                                    allAliasesToAdd.Add(new SubjectAlias
                                    {
                                        SubjectId = kvp.Key,
                                        Name = aliasName,
                                        LanguageCode = aliasLang
                                    });
                                }
                            }
                        }
                        
                        // Save aliases if any were added
                        if (allAliasesToAdd.Count > 0)
                        {
                            context.SubjectAliases.AddRange(allAliasesToAdd);
                            await context.SaveChangesAsync(cancellationToken);
                            logger.LogDebug($"💾 {university.Name}: Saved {allAliasesToAdd.Count} alias(es)");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"❌ Failed to save subjects/aliases for {university.Name}: {ex.Message}");
                    }
                }

                // Save all added programs in one transaction
                if (programsToAdd.Count > 0)
                {
                    try
                    {
                        context.Programs.AddRange(programsToAdd);
                        await context.SaveChangesAsync(cancellationToken);
                        logger.LogInformation($"💾 {university.Name}: Saved {programsAdded} new program(s), {programsSkipped} already existed, {programsWithNoSubjects} had no subjects");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"❌ Failed to save programs for {university.Name}: {ex.Message}");
                        // Don't count as added if save failed
                        programsAdded = 0;
                    }
                }
                else if (programsSkipped > 0)
                {
                    logger.LogInformation($"⏭ {university.Name}: All {programsSkipped} program(s) already exist in database (skipped - reason: duplicates)");
                }
                else if (programsWithNoSubjects > 0)
                {
                    logger.LogWarning($"⚠ {university.Name}: {programsWithNoSubjects} program(s) had no subject areas and were skipped (skipped - reason: no subjects)");
                }

                result.ProgramsAdded = programsAdded;
                result.ProgramsSkipped = programsSkipped;

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ Error syncing programs for university {university.Id} ({university.Name}): {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Fallback method: Runs subject inference when HEI API returns no programs
        /// Creates inferred programs with IsInferred = true
        /// </summary>
        private async Task<SyncProgramsResult> RunSubjectInferenceFallbackAsync(
            University university,
            ApplicationDbContext context,
            ILogger<HeiApiService> logger,
            ISubjectInferenceService? inferenceService = null)
        {
            var result = new SyncProgramsResult();

            if (inferenceService == null)
            {
                logger.LogWarning($"⚠ {university.Name}: Subject inference service not available. Skipping inference.");
                return result;
            }

            logger.LogInformation($"🔍 No programs found in HEI for {university.Name}. Running subject inference...");

            try
            {
                // Infer subjects from university name and description
                var inferredSubjectNames = await inferenceService.InferSubjectsAsync(
                    university.Name,
                    university.Description);

                if (inferredSubjectNames == null || inferredSubjectNames.Count == 0)
                {
                    logger.LogInformation($"ℹ {university.Name}: No subjects inferred from name/description");
                    return result;
                }

                logger.LogInformation($"✅ Inferred subjects for {university.Name}: {string.Join(", ", inferredSubjectNames)}");

                int programsAdded = 0;
                int programsSkipped = 0;

                // Create programs for each inferred subject
                foreach (var subjectName in inferredSubjectNames)
                {
                    try
                    {
                        // Get or create subject
                        var subject = await GetOrCreateSubjectWithAliasesAsync(
                            subjectName,
                            "unknown", // Language unknown for inferred subjects
                            context,
                            logger,
                            null);

                        // Check if inferred program already exists
                        var programExists = await context.Programs
                            .AnyAsync(p => p.UniversityId == university.Id &&
                                          p.SubjectId == subject.Id &&
                                          p.IsInferred == true);

                        if (programExists)
                        {
                            programsSkipped++;
                            logger.LogDebug($"⏭ {university.Name}: Inferred program for subject '{subjectName}' already exists");
                            continue;
                        }

                        // Create inferred program
                        var inferredProgram = new UniversityProgram
                        {
                            UniversityId = university.Id,
                            SubjectId = subject.Id,
                            Name = $"{subjectName} Program", // Generic name for inferred programs
                            DegreeType = null,
                            Duration = null,
                            Language = null,
                            Description = $"Inferred program based on university name and description.",
                            IsInferred = true // ✅ Flag: this is an inferred program
                        };

                        context.Programs.Add(inferredProgram);
                        programsAdded++;
                        logger.LogDebug($"➕ {university.Name}: Added inferred program for subject '{subjectName}'");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"❌ Error creating inferred program for subject '{subjectName}' ({university.Name}): {ex.Message}");
                    }
                }

                // Save all inferred programs
                if (programsAdded > 0)
                {
                    await context.SaveChangesAsync();
                    logger.LogInformation($"💾 Saved {programsAdded} inferred program(s) for {university.Name} ({programsSkipped} already existed)");
                }
                else if (programsSkipped > 0)
                {
                    logger.LogInformation($"⏭ {university.Name}: All {programsSkipped} inferred program(s) already exist");
                }

                result.ProgramsAdded = programsAdded;
                result.ProgramsSkipped = programsSkipped;

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ Error running subject inference for {university.Name}: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Gets or creates a subject with all its aliases (language variants) - BATCHED VERSION
        /// This ensures comprehensive language-agnostic searching and reduces database calls
        /// </summary>
        private async Task<Subject> GetOrCreateSubjectWithAliasesBatchedAsync(
            string primarySubjectName, 
            string primaryLanguage,
            ApplicationDbContext context,
            ILogger<HeiApiService> logger,
            Dictionary<string, Subject> subjectsToCreate,
            Dictionary<int, List<(string Name, string Language)>> subjectAliasesToAdd,
            List<DTOs.SubjectAreaLabel>? relatedLabels = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(primarySubjectName))
            {
                primarySubjectName = "Unknown";
            }

            var normalizedName = primarySubjectName.Trim();

            // Check if already in our batch dictionary
            if (subjectsToCreate.TryGetValue(normalizedName, out var batchedSubject))
            {
                // Still need to queue aliases
                var subjectId = batchedSubject.Id > 0 ? batchedSubject.Id : 0; // Will be set after SaveChanges
                if (!subjectAliasesToAdd.ContainsKey(subjectId))
                {
                    subjectAliasesToAdd[subjectId] = new List<(string, string)>();
                }
                var normalizedLang = string.IsNullOrWhiteSpace(primaryLanguage) ? "unknown" : primaryLanguage.Trim().ToLowerInvariant();
                subjectAliasesToAdd[subjectId].Add((normalizedName, normalizedLang));
                
                if (relatedLabels != null)
                {
                    foreach (var label in relatedLabels)
                    {
                        if (!string.IsNullOrWhiteSpace(label.Name))
                        {
                            var labelLang = string.IsNullOrWhiteSpace(label.Language) ? "unknown" : label.Language.Trim().ToLowerInvariant();
                            subjectAliasesToAdd[subjectId].Add((label.Name.Trim(), labelLang));
                        }
                    }
                }
                
                return batchedSubject;
            }

            // Try to find existing subject by name or by alias
            var subject = await context.Subjects
                .Include(s => s.Aliases)
                .FirstOrDefaultAsync(s => s.Name == normalizedName ||
                                         s.Aliases.Any(a => a.Name == normalizedName), cancellationToken);

            if (subject == null)
            {
                // Determine category based on subject name (basic heuristic)
                var category = DetermineSubjectCategory(normalizedName);

                subject = new Subject
                {
                    Name = normalizedName,
                    Category = category,
                    Description = null
                };

                context.Subjects.Add(subject);
                subjectsToCreate[normalizedName] = subject; // Track in batch
                logger.LogDebug($"➕ Queued subject: {normalizedName} (Category: {category})");
            }

            // Queue aliases for batch addition
            var sid = subject.Id > 0 ? subject.Id : 0; // For new subjects, will be set after SaveChanges
            if (!subjectAliasesToAdd.ContainsKey(sid))
            {
                subjectAliasesToAdd[sid] = new List<(string, string)>();
            }
            
            var normalizedLang2 = string.IsNullOrWhiteSpace(primaryLanguage) ? "unknown" : primaryLanguage.Trim().ToLowerInvariant();
            subjectAliasesToAdd[sid].Add((normalizedName, normalizedLang2));

            // Add related labels as aliases (these are other language variants of the same subject)
            if (relatedLabels != null && relatedLabels.Count > 0)
            {
                foreach (var label in relatedLabels)
                {
                    if (!string.IsNullOrWhiteSpace(label.Name))
                    {
                        var labelLang = string.IsNullOrWhiteSpace(label.Language) ? "unknown" : label.Language.Trim().ToLowerInvariant();
                        subjectAliasesToAdd[sid].Add((label.Name.Trim(), labelLang));
                    }
                }
            }

            return subject;
        }

        /// <summary>
        /// Gets or creates a subject with all its aliases (language variants)
        /// This ensures comprehensive language-agnostic searching
        /// </summary>
        private async Task<Subject> GetOrCreateSubjectWithAliasesAsync(
            string primarySubjectName, 
            string primaryLanguage,
            ApplicationDbContext context,
            ILogger<HeiApiService> logger,
            List<DTOs.SubjectAreaLabel>? relatedLabels = null)
        {
            if (string.IsNullOrWhiteSpace(primarySubjectName))
            {
                primarySubjectName = "Unknown";
            }

            var normalizedName = primarySubjectName.Trim();

            // Try to find existing subject by name or by alias
            var subject = await context.Subjects
                .Include(s => s.Aliases)
                .FirstOrDefaultAsync(s => s.Name == normalizedName ||
                                         s.Aliases.Any(a => a.Name == normalizedName));

            if (subject == null)
            {
                // Determine category based on subject name (basic heuristic)
                var category = DetermineSubjectCategory(normalizedName);

                subject = new Subject
                {
                    Name = normalizedName,
                    Category = category,
                    Description = null
                };

                context.Subjects.Add(subject);
                await context.SaveChangesAsync();

                logger.LogDebug($"✅ Created subject: {normalizedName} (Category: {category})");
            }

            // Ensure the primary name is stored as an alias if it's not the main name
            if (subject.Name != normalizedName)
            {
                await EnsureSubjectAliasAsync(subject.Id, normalizedName, primaryLanguage, context, logger);
            }
            else
            {
                // Ensure the main name is also stored as an alias for consistency
                await EnsureSubjectAliasAsync(subject.Id, normalizedName, primaryLanguage, context, logger);
            }

            // Add related labels as aliases (these are other language variants of the same subject)
            if (relatedLabels != null && relatedLabels.Count > 0)
            {
                foreach (var label in relatedLabels)
                {
                    if (!string.IsNullOrWhiteSpace(label.Name))
                    {
                        await EnsureSubjectAliasAsync(subject.Id, label.Name.Trim(), label.Language, context, logger);
                    }
                }
            }

            return subject;
        }

        /// <summary>
        /// Ensures a subject alias exists for the given subject
        /// </summary>
        private async Task EnsureSubjectAliasAsync(int subjectId, string aliasName, string languageCode, ApplicationDbContext context, ILogger<HeiApiService> logger)
        {
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                return;
            }

            var normalizedAlias = aliasName.Trim();
            var normalizedLang = string.IsNullOrWhiteSpace(languageCode) ? "unknown" : languageCode.Trim().ToLowerInvariant();

            // Check if alias already exists
            var aliasExists = await context.SubjectAliases
                .AnyAsync(a => a.SubjectId == subjectId &&
                              a.Name == normalizedAlias &&
                              a.LanguageCode == normalizedLang);

            if (!aliasExists)
            {
                var alias = new SubjectAlias
                {
                    SubjectId = subjectId,
                    Name = normalizedAlias,
                    LanguageCode = normalizedLang
                };

                context.SubjectAliases.Add(alias);
                await context.SaveChangesAsync();

                logger.LogDebug($"✅ Added alias: {normalizedAlias} ({normalizedLang}) for subject ID {subjectId}");
            }
        }

        /// <summary>
        /// Gets or creates a subject by name (legacy method for backward compatibility)
        /// </summary>
        private async Task<Subject> GetOrCreateSubjectAsync(string subjectName)
        {
            return await GetOrCreateSubjectWithAliasesAsync(subjectName, "unknown", _context, _logger, null);
        }

        /// <summary>
        /// Determines subject category based on name (basic heuristic)
        /// </summary>
        private static string? DetermineSubjectCategory(string subjectName)
        {
            var lower = subjectName.ToLowerInvariant();

            if (lower.Contains("engineering") || lower.Contains("ingenier") || lower.Contains("ingénier") ||
                lower.Contains("computer") || lower.Contains("informatics") || lower.Contains("informatics") ||
                lower.Contains("mathematics") || lower.Contains("math") || lower.Contains("physics") ||
                lower.Contains("chemistry") || lower.Contains("biology") || lower.Contains("technology"))
            {
                return "STEM";
            }

            if (lower.Contains("medicine") || lower.Contains("médecine") || lower.Contains("medizin") ||
                lower.Contains("health") || lower.Contains("nursing") || lower.Contains("pharmacy"))
            {
                return "Health Sciences";
            }

            if (lower.Contains("business") || lower.Contains("economics") || lower.Contains("économie") ||
                lower.Contains("management") || lower.Contains("finance") || lower.Contains("commerce"))
            {
                return "Business & Economics";
            }

            if (lower.Contains("law") || lower.Contains("droit") || lower.Contains("recht") ||
                lower.Contains("legal"))
            {
                return "Law";
            }

            if (lower.Contains("arts") || lower.Contains("art") || lower.Contains("music") ||
                lower.Contains("design") || lower.Contains("theater") || lower.Contains("theatre"))
            {
                return "Arts";
            }

            if (lower.Contains("education") || lower.Contains("éducation") || lower.Contains("pädagogik") ||
                lower.Contains("teaching") || lower.Contains("pedagogy"))
            {
                return "Education";
            }

            return "Other";
        }

        /// <summary>
        /// Fetches programs for a specific HEI ID with retry logic
        /// </summary>
        private async Task<List<HeiApiProgram>> FetchProgramsByHeiIdWithRetryAsync(string heiId, ILogger<HeiApiService>? logger = null, CancellationToken cancellationToken = default)
        {
            logger ??= _logger;
            
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = await FetchProgramsByHeiId(heiId, logger, cancellationToken);
                    if (result != null && result.Count > 0)
                    {
                        return result;
                    }
                    // If result is empty, don't retry - it's not a transient error
                    return result ?? new List<HeiApiProgram>();
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    var delay = RetryBaseDelayMs * (int)Math.Pow(2, attempt - 1);
                    logger.LogWarning($"⚠ Retry {attempt}/{MaxRetries} for HEI ID {heiId} after {delay}ms: {ex.Message}");
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TaskCanceledException) when (attempt < MaxRetries)
                {
                    var delay = RetryBaseDelayMs * (int)Math.Pow(2, attempt - 1);
                    logger.LogWarning($"⚠ Timeout on attempt {attempt}/{MaxRetries} for HEI ID {heiId}, retrying after {delay}ms");
                    await Task.Delay(delay, cancellationToken);
                }
            }
            
            // Final attempt without retry logic
            return await FetchProgramsByHeiId(heiId, logger, cancellationToken);
        }

        /// <summary>
        /// Fetches programs for a specific HEI ID
        /// </summary>
        private async Task<List<HeiApiProgram>> FetchProgramsByHeiId(string heiId, ILogger<HeiApiService>? logger = null, CancellationToken cancellationToken = default)
        {
            logger ??= _logger;
            
            try
            {
                // Try common HEI API endpoints for programs
                var endpoints = new[]
                {
                    $"/api/public/v1/hei/{heiId}/programs",
                    $"/api/public/v1/programs?hei_id={heiId}",
                    $"/api/public/v1/hei/{heiId}/qualifications"
                };

                foreach (var endpoint in endpoints)
                {
                    try
                    {
                        var fullUrl = $"{BaseUrl}{endpoint}";
                        logger.LogDebug($"🌐 GET {fullUrl}");

                        var response = await _httpClient.GetAsync(endpoint, cancellationToken);

                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();

                            if (string.IsNullOrWhiteSpace(json))
                            {
                                continue;
                            }

                            var jsonOptions = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true,
                                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                            };

                            // Try wrapped response format
                            try
                            {
                                var wrappedResponse = JsonSerializer.Deserialize<HeiApiProgramResponse>(json, jsonOptions);
                                if (wrappedResponse?.Data != null && wrappedResponse.Data.Count > 0)
                                {
                                    logger.LogInformation($"✅ Found {wrappedResponse.Data.Count} programs using endpoint {endpoint}");
                                    return wrappedResponse.Data;
                                }
                            }
                            catch (JsonException)
                            {
                                // Try direct array format
                            }

                            // Try direct array format
                            try
                            {
                                var directArray = JsonSerializer.Deserialize<List<HeiApiProgram>>(json, jsonOptions);
                                if (directArray != null && directArray.Count > 0)
                                {
                                    logger.LogInformation($"✅ Found {directArray.Count} programs using endpoint {endpoint}");
                                    return directArray;
                                }
                            }
                            catch (JsonException)
                            {
                                // Continue to next endpoint
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug($"⚠ Endpoint {endpoint} failed: {ex.Message}");
                        continue;
                    }
                }

                logger.LogWarning($"⚠ No programs found for HEI ID {heiId} using any endpoint");
                return new List<HeiApiProgram>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ Error fetching programs for HEI ID {heiId}: {ex.Message}");
                return new List<HeiApiProgram>();
            }
        }

        /// <summary>
        /// Fetches universities by country with retry logic
        /// </summary>
        private async Task<List<HeiApiUniversity>> FetchUniversitiesByCountryWithRetryAsync(string countryCode, CancellationToken cancellationToken = default)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = await FetchUniversitiesByCountry(countryCode, cancellationToken);
                    if (result != null && result.Count > 0)
                    {
                        return result;
                    }
                    // If result is empty, don't retry - it's not a transient error
                    return result ?? new List<HeiApiUniversity>();
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    var delay = RetryBaseDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogWarning($"⚠ Retry {attempt}/{MaxRetries} for country {countryCode} after {delay}ms: {ex.Message}");
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TaskCanceledException) when (attempt < MaxRetries)
                {
                    var delay = RetryBaseDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogWarning($"⚠ Timeout on attempt {attempt}/{MaxRetries} for country {countryCode}, retrying after {delay}ms");
                    await Task.Delay(delay, cancellationToken);
                }
            }
            
            // Final attempt without retry logic
            return await FetchUniversitiesByCountry(countryCode, cancellationToken);
        }

        private async Task<List<HeiApiUniversity>> FetchUniversitiesByCountry(string countryCode, CancellationToken cancellationToken = default)
        {
            try
            {
                var endpoint = $"/api/public/v1/country/{countryCode}/hei";
                var fullUrl = $"{BaseUrl}{endpoint}";
                
                _logger.LogInformation($"🌐 GET {fullUrl}");

                var response = await _httpClient.GetAsync(endpoint, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"❌ HTTP {response.StatusCode} for {countryCode}");
                    _logger.LogWarning($"   Response: {errorContent.Substring(0, Math.Min(500, errorContent.Length))}");
                    return new List<HeiApiUniversity>();
                }

                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"📥 Received JSON: {json.Length} characters");

                if (string.IsNullOrWhiteSpace(json))
                {
                    _logger.LogWarning($"⚠ Empty JSON response for {countryCode}");
                    return new List<HeiApiUniversity>();
                }

                // Log first 1000 chars to see structure
                _logger.LogInformation($"🔍 JSON starts with: {json.Substring(0, Math.Min(1000, json.Length))}");

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                // Try to deserialize as HeiApiResponse first
                HeiApiResponse? result = null;
                List<HeiApiUniversity>? directArray = null;

                try
                {
                    // First try: Deserialize as wrapped response
                    result = JsonSerializer.Deserialize<HeiApiResponse>(json, jsonOptions);
                    
                    if (result != null && result.Data != null && result.Data.Count > 0)
                    {
                        _logger.LogInformation($"✅ Parsed {result.Data.Count} universities (wrapped format)");
                        return result.Data;
                    }
                }
                catch (JsonException)
                {
                    // If that fails, try direct array
                    _logger.LogDebug("⚠ Wrapped format failed, trying direct array...");
                }

                // Second try: Deserialize as direct array
                try
                {
                    directArray = JsonSerializer.Deserialize<List<HeiApiUniversity>>(json, jsonOptions);
                    
                    if (directArray != null && directArray.Count > 0)
                    {
                        _logger.LogInformation($"✅ Parsed {directArray.Count} universities (direct array format)");
                        return directArray;
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, $"❌ JSON deserialization failed for {countryCode}");
                    _logger.LogError($"   Error: {jsonEx.Message}");
                    _logger.LogError($"   Path: {jsonEx.Path}");
                    _logger.LogError($"📄 Failed JSON (first 2000 chars): {json.Substring(0, Math.Min(2000, json.Length))}");
                    return new List<HeiApiUniversity>();
                }

                // If both failed or returned empty
                if ((result == null || result.Data == null || result.Data.Count == 0) && 
                    (directArray == null || directArray.Count == 0))
                {
                    _logger.LogWarning($"⚠ No universities found in JSON for {countryCode}");
                    _logger.LogWarning($"   Result.Data is null: {result?.Data == null}");
                    _logger.LogWarning($"   DirectArray is null: {directArray == null}");
                    _logger.LogWarning($"   JSON preview: {json.Substring(0, Math.Min(500, json.Length))}");
                    return new List<HeiApiUniversity>();
                }

                // Return whichever worked
                if (directArray != null && directArray.Count > 0)
                {
                    return directArray;
                }

                if (result?.Data != null && result.Data.Count > 0)
                {
                    return result.Data;
                }

                return new List<HeiApiUniversity>();
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, $"🔥 HTTP request error for {countryCode}: {httpEx.Message}");
                return new List<HeiApiUniversity>();
            }
            catch (TaskCanceledException timeoutEx)
            {
                _logger.LogError(timeoutEx, $"⏱ Request timeout for {countryCode}");
                return new List<HeiApiUniversity>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"🔥 Unexpected error fetching {countryCode}: {ex.Message}");
                _logger.LogError($"   Stack trace: {ex.StackTrace}");
                return new List<HeiApiUniversity>();
            }
        }

        /// <summary>
        /// Resets stale running sync status if it has been running for more than the timeout period
        /// This prevents sync from getting stuck forever if the process crashes
        /// </summary>
        private async Task ResetStaleSyncStatusIfNeededAsync(
            ApplicationDbContext context,
            string syncType,
            ILogger<HeiApiService> logger)
        {
            try
            {
                var staleStatus = await context.SyncStatuses
                    .FirstOrDefaultAsync(s => s.SyncType == syncType && s.IsRunning == true);

                if (staleStatus != null && staleStatus.StartedAt.HasValue)
                {
                    var runningDuration = DateTime.UtcNow - staleStatus.StartedAt.Value;
                    if (runningDuration.TotalMinutes >= SyncTimeoutMinutes)
                    {
                        logger.LogWarning($"⚠ Detected stale running sync status for {syncType} (running for {runningDuration.TotalMinutes:F1} minutes). Auto-resetting...");
                        await ResetStaleSyncStatusAsync(context, syncType, logger);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ Error checking for stale sync status: {ex.Message}");
                // Don't throw - this is a safeguard, not critical path
            }
        }

        /// <summary>
        /// Resets a stale or stuck sync status to stopped state
        /// Used for cleanup and recovery from crashes
        /// </summary>
        private async Task ResetStaleSyncStatusAsync(
            ApplicationDbContext context,
            string syncType,
            ILogger<HeiApiService> logger)
        {
            try
            {
                var status = await context.SyncStatuses
                    .FirstOrDefaultAsync(s => s.SyncType == syncType);

                if (status != null)
                {
                    var wasRunning = status.IsRunning;
                    status.IsRunning = false;
                    
                    if (!status.CompletedAt.HasValue)
                    {
                        status.CompletedAt = DateTime.UtcNow;
                    }

                    if (wasRunning)
                    {
                        status.LastMessage = status.LastMessage?.Contains("Reset") == true
                            ? status.LastMessage
                            : $"⚠ Sync was reset (previous state was stuck or stale)";
                    }

                    await context.SaveChangesAsync();
                    logger.LogInformation($"✅ Reset sync status for {syncType}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"❌ Error resetting stale sync status: {ex.Message}");
                throw; // Re-throw so caller knows it failed
            }
        }

        /// <summary>
        /// Public method to check and reset stale sync statuses on startup
        /// Can be called from AdminController or during application startup
        /// </summary>
        public async Task CheckAndResetStaleSyncStatusesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<HeiApiService>>();

            await ResetStaleSyncStatusIfNeededAsync(context, "Programs", logger);
        }
    }
}
