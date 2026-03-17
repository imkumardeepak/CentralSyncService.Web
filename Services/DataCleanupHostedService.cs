using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Web.Core.Interfaces;

namespace Web.Services
{
    /// <summary>
    /// Periodically removes historical sync and box-tracking data so live sync queries stay fast.
    /// </summary>
    public class DataCleanupHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<DataCleanupHostedService> _logger;
        private readonly bool _enabled;
        private readonly int _retentionDays;
        private readonly TimeSpan _interval;
        private readonly TimeSpan _initialDelay;

        public DataCleanupHostedService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DataCleanupHostedService> logger,
            IConfiguration configuration)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _enabled = configuration.GetValue("Cleanup:Enabled", true);
            _retentionDays = Math.Max(1, configuration.GetValue("Cleanup:RetentionDays", 30));
            _interval = TimeSpan.FromHours(Math.Max(1, configuration.GetValue("Cleanup:IntervalHours", 12)));
            _initialDelay = TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Cleanup:InitialDelayMinutes", 10)));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Data cleanup service is disabled.");
                return;
            }

            _logger.LogInformation(
                "Data cleanup service started. RetentionDays={RetentionDays}, IntervalHours={IntervalHours}, InitialDelayMinutes={InitialDelayMinutes}",
                _retentionDays,
                _interval.TotalHours,
                _initialDelay.TotalMinutes);

            try
            {
                await Task.Delay(_initialDelay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var syncRepository = scope.ServiceProvider.GetRequiredService<ISyncRepository>();
                        var cleanupResult = await syncRepository.CleanupHistoricalDataAsync(_retentionDays, stoppingToken).ConfigureAwait(false);

                        _logger.LogInformation(
                            "Data cleanup completed. Deleted {SorterScansDeleted} SorterScans_Sync rows older than {CutoffDate:yyyy-MM-dd}.",
                            cleanupResult.SorterScansDeleted,
                            cleanupResult.CutoffDate);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Data cleanup failed.");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
