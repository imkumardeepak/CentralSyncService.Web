using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Web.Infrastructure.Logging;

namespace Web.Infrastructure.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFileLogger _fileLogger;

        public RequestLoggingMiddleware(RequestDelegate next, IFileLogger fileLogger)
        {
            _next = next;
            _fileLogger = fileLogger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestPath = context.Request.Path;
            var method = context.Request.Method;
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            
            // Skip logging for static files
            if (IsStaticFile(requestPath))
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            _fileLogger.LogInformation($"Request Started: {method} {requestPath} | IP: {ipAddress}");

            try
            {
                await _next(context).ConfigureAwait(false);
                
                stopwatch.Stop();
                var statusCode = context.Response.StatusCode;
                
                if (statusCode >= 400)
                {
                    _fileLogger.LogWarning($"Request Completed: {method} {requestPath} | Status: {statusCode} | Duration: {stopwatch.ElapsedMilliseconds}ms");
                }
                else
                {
                    _fileLogger.LogInformation($"Request Completed: {method} {requestPath} | Status: {statusCode} | Duration: {stopwatch.ElapsedMilliseconds}ms");
                }

                // Log page visits for MVC pages
                if (IsMvcPage(requestPath))
                {
                    var userId = context.User?.Identity?.Name ?? "Anonymous";
                    _fileLogger.LogPageVisit(requestPath, userId, ipAddress);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _fileLogger.LogError($"Request Failed: {method} {requestPath} | Duration: {stopwatch.ElapsedMilliseconds}ms", ex);
                throw;
            }
        }

        private bool IsStaticFile(string path)
        {
            var staticExtensions = new[] { ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot" };
            foreach (var ext in staticExtensions)
            {
                if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private bool IsMvcPage(string path)
        {
            // Skip API endpoints and static files
            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
                return false;
            
            // Consider paths without dots as MVC pages
            return !path.Contains(".");
        }
    }

    public class ExceptionLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFileLogger _fileLogger;

        public ExceptionLoggingMiddleware(RequestDelegate next, IFileLogger fileLogger)
        {
            _next = next;
            _fileLogger = fileLogger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var requestPath = context.Request.Path;
                var method = context.Request.Method;
                
                _fileLogger.LogCritical($"Unhandled Exception in {method} {requestPath}", ex);
                
                // Re-throw to let the default exception handler deal with it
                throw;
            }
        }
    }
}
