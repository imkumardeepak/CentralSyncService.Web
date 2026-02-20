using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Web.Infrastructure.Logging
{
    public interface IFileLogger
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, Exception exception = null);
        void LogDebug(string message);
        void LogCritical(string message, Exception exception = null);
        void LogPageVisit(string pageName, string userId = null, string ipAddress = null);
        void LogDatabaseOperation(string operation, string table, bool success, string details = null);
        void LogSyncOperation(string plantCode, int recordsProcessed, bool success, string message = null);
    }

    public class FileLoggerOptions
    {
        public string LogDirectory { get; set; } = "Logs";
        public int RetentionDays { get; set; } = 2;
        public long MaxFileSizeMB { get; set; } = 10;
        public bool EnableConsoleOutput { get; set; } = true;
    }

    public class FileLogger : IFileLogger
    {
        private readonly FileLoggerOptions _options;
        private readonly string _category;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private string _currentLogFile;
        private DateTime _currentDate;

        public FileLogger(IOptions<FileLoggerOptions> options, string category = null)
        {
            _options = options.Value;
            _category = category ?? "Application";
            InitializeLogDirectory();
            UpdateCurrentLogFile();
        }

        private void InitializeLogDirectory()
        {
            if (!Directory.Exists(_options.LogDirectory))
            {
                Directory.CreateDirectory(_options.LogDirectory);
            }
        }

        private void UpdateCurrentLogFile()
        {
            var today = DateTime.Now.Date;
            if (_currentDate != today || string.IsNullOrEmpty(_currentLogFile))
            {
                _currentDate = today;
                var fileName = $"log_{today:yyyyMMdd}.txt";
                _currentLogFile = Path.Combine(_options.LogDirectory, fileName);
            }
        }

        private async Task WriteLogAsync(string level, string message, Exception exception = null)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                UpdateCurrentLogFile();
                
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = new StringBuilder();
                logEntry.AppendLine($"[{timestamp}] [{level}] [{_category}] {message}");
                
                if (exception != null)
                {
                    logEntry.AppendLine($"Exception: {exception.Message}");
                    logEntry.AppendLine($"StackTrace: {exception.StackTrace}");
                }
                
                await File.AppendAllTextAsync(_currentLogFile, logEntry.ToString());
                
                if (_options.EnableConsoleOutput)
                {
                    Console.WriteLine(logEntry.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void LogInformation(string message)
        {
            WriteLogAsync("INFO", message).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void LogWarning(string message)
        {
            WriteLogAsync("WARN", message).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void LogError(string message, Exception exception = null)
        {
            WriteLogAsync("ERROR", message, exception).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void LogDebug(string message)
        {
            WriteLogAsync("DEBUG", message).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void LogCritical(string message, Exception exception = null)
        {
            WriteLogAsync("CRITICAL", message, exception).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void LogPageVisit(string pageName, string userId = null, string ipAddress = null)
        {
            var message = $"Page Visit: {pageName}";
            if (!string.IsNullOrEmpty(userId))
                message += $" | User: {userId}";
            if (!string.IsNullOrEmpty(ipAddress))
                message += $" | IP: {ipAddress}";
            
            WriteLogAsync("PAGE", message).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void LogDatabaseOperation(string operation, string table, bool success, string details = null)
        {
            var message = $"DB {operation} on {table} - {(success ? "SUCCESS" : "FAILED")}";
            if (!string.IsNullOrEmpty(details))
                message += $" | {details}";
            
            WriteLogAsync("DB", message).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void LogSyncOperation(string plantCode, int recordsProcessed, bool success, string message = null)
        {
            var logMessage = $"SYNC Plant: {plantCode} | Records: {recordsProcessed} | Status: {(success ? "SUCCESS" : "FAILED")}";
            if (!string.IsNullOrEmpty(message))
                logMessage += $" | {message}";
            
            WriteLogAsync("SYNC", logMessage).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }

    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly FileLoggerOptions _options;

        public FileLoggerProvider(IOptions<FileLoggerOptions> options)
        {
            _options = options.Value;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLoggerAdapter(new FileLogger(Options.Create(_options), categoryName));
        }

        public void Dispose()
        {
        }

        private class FileLoggerAdapter : ILogger
        {
            private readonly FileLogger _fileLogger;

            public FileLoggerAdapter(FileLogger fileLogger)
            {
                _fileLogger = fileLogger;
            }

            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var message = formatter(state, exception);
                
                switch (logLevel)
                {
                    case LogLevel.Debug:
                        _fileLogger.LogDebug(message);
                        break;
                    case LogLevel.Information:
                        _fileLogger.LogInformation(message);
                        break;
                    case LogLevel.Warning:
                        _fileLogger.LogWarning(message);
                        break;
                    case LogLevel.Error:
                        _fileLogger.LogError(message, exception);
                        break;
                    case LogLevel.Critical:
                        _fileLogger.LogCritical(message, exception);
                        break;
                }
            }
        }
    }

    public class LogCleanupService : BackgroundService
    {
        private readonly FileLoggerOptions _options;
        private readonly ILogger<LogCleanupService> _logger;

        public LogCleanupService(IOptions<FileLoggerOptions> options, ILogger<LogCleanupService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    CleanupOldLogs();
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken).ConfigureAwait(false); // Check every hour
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during log cleanup");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken).ConfigureAwait(false);
                }
            }
        }

        private void CleanupOldLogs()
        {
            try
            {
                if (!Directory.Exists(_options.LogDirectory))
                    return;

                var cutoffDate = DateTime.Now.Date.AddDays(-_options.RetentionDays);
                var logFiles = Directory.GetFiles(_options.LogDirectory, "log_*.txt");
                
                foreach (var file in logFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.CreationTime.Date < cutoffDate)
                        {
                            fileInfo.Delete();
                            _logger.LogInformation($"Deleted old log file: {fileInfo.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to delete log file: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during log cleanup");
            }
        }
    }

    public static class FileLoggerExtensions
    {
        public static IServiceCollection AddFileLogging(this IServiceCollection services, Action<FileLoggerOptions> configure = null)
        {
            services.Configure(configure ?? (opts => { }));
            services.AddSingleton<IFileLogger>(provider => 
            {
                var options = provider.GetRequiredService<IOptions<FileLoggerOptions>>();
                return new FileLogger(options, "Application");
            });
            services.AddHostedService<LogCleanupService>();
            services.AddLogging(builder =>
            {
                builder.AddProvider(new FileLoggerProvider(
                    Microsoft.Extensions.Options.Options.Create(new FileLoggerOptions())));
            });
            return services;
        }
    }
}
