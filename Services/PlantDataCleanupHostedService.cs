using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Web.Core.Interfaces;

namespace Web.Services
{
    /// <summary>
    /// Periodically removes historical data from local plant databases, keeping only last 7 days.
    /// Runs daily at 18:00 (6 PM).
    /// </summary>
    public class PlantDataCleanupHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PlantDataCleanupHostedService> _logger;
        private readonly bool _enabled;
        private readonly int _retentionDays;
        private readonly int _runAtHour;

        public PlantDataCleanupHostedService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<PlantDataCleanupHostedService> logger,
            IConfiguration configuration)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _enabled = configuration.GetValue("PlantCleanup:Enabled", true);
            _retentionDays = Math.Max(1, configuration.GetValue("PlantCleanup:RetentionDays", 7));
            _runAtHour = configuration.GetValue("PlantCleanup:RunAtHour", 18); // Default 6 PM
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Plant data cleanup service is disabled.");
                return;
            }

            _logger.LogInformation(
                "Plant data cleanup service started. RetentionDays={RetentionDays}, RunAtHour={RunAtHour}",
                _retentionDays,
                _runAtHour);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Calculate time until next run at 18:00
                    var now = DateTime.Now;
                    var nextRun = new DateTime(now.Year, now.Month, now.Day, _runAtHour, 0, 0);
                    
                    if (now > nextRun)
                    {
                        // If past 6 PM today, schedule for tomorrow
                        nextRun = nextRun.AddDays(1);
                    }

                    var delay = nextRun - now;
                    _logger.LogInformation("Next plant data cleanup scheduled at {NextRun:yyyy-MM-dd HH:mm}", nextRun);

                    try
                    {
                        await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    // Run cleanup
                    await CleanupPlantDataAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Plant data cleanup failed. Will retry next scheduled time.");
                    
                    // Wait 1 hour before retrying on error
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task CleanupPlantDataAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var plantRepository = scope.ServiceProvider.GetRequiredService<IPlantRepository>();

            _logger.LogInformation("Starting plant data cleanup for records older than {RetentionDays} days", _retentionDays);

            try
            {
                var result = await plantRepository.CleanupOldDataAsync(_retentionDays, stoppingToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "Plant data cleanup completed. PlantsProcessed={PlantsProcessed}, TablesCleaned={TablesCleaned}, TotalRowsDeleted={TotalRowsDeleted}",
                    result.PlantsProcessed,
                    result.TablesCleaned,
                    result.TotalRowsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during plant data cleanup");
                throw;
            }
        }
    }
}
