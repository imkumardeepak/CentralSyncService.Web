using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Web.Services
{
    /// <summary>
    /// Hosted service that starts/stops the SyncService when the ASP.NET Core app runs.
    /// Includes automatic restart logic if the sync service stops unexpectedly.
    /// </summary>
    public class SyncHostedService : BackgroundService
    {
        private readonly SyncService _syncService;
        private readonly ILogger<SyncHostedService> _logger;
        private readonly TimeSpan _restartDelay = TimeSpan.FromSeconds(10);

        public SyncHostedService(SyncService syncService, ILogger<SyncHostedService> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SyncHostedService: Host started. Attempting immediate sync service startup.");

            try
            {
                if (!_syncService.IsRunning)
                {
                    _syncService.Start();
                    _logger.LogInformation("SyncHostedService: Sync service started successfully.");
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "SyncHostedService: Immediate startup failed. Watchdog will retry in {RestartDelay}s.", _restartDelay.TotalSeconds);
            }

            // Main watchdog loop - keeps sync service running
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!_syncService.IsRunning)
                    {
                        _logger.LogWarning("SyncHostedService: Sync service is not running. Attempting to start...");
                        _syncService.Start();
                        _logger.LogInformation("SyncHostedService: Sync service started successfully.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SyncHostedService: Failed to start sync service. Will retry in {RestartDelay}s.", _restartDelay.TotalSeconds);
                }

                try
                {
                    // Check every 10 seconds if service is still running
                    await Task.Delay(_restartDelay, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("SyncHostedService: Cancellation requested, stopping watchdog.");
                    break;
                }
            }

            // Stop sync service when application is shutting down
            try
            {
                if (_syncService.IsRunning)
                {
                    _logger.LogInformation("SyncHostedService: Stopping sync service...");
                    _syncService.Stop();
                    _logger.LogInformation("SyncHostedService: Sync service stopped.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SyncHostedService: Error stopping sync service.");
            }
        }
    }
}
