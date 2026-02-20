using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Web.Services
{
    /// <summary>
    /// Hosted service that starts/stops the SyncService when the ASP.NET Core app runs.
    /// </summary>
    public class SyncHostedService : BackgroundService
    {
        private readonly SyncService _syncService;

        public SyncHostedService(SyncService syncService)
        {
            _syncService = syncService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait 3 minutes before starting the sync service as requested
            try
            {
                await Task.Delay(System.TimeSpan.FromMinutes(3), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // App is stopping, do nothing
                return;
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                _syncService.Start();
                stoppingToken.Register(() => _syncService.Stop());
            }
        }
    }
}
