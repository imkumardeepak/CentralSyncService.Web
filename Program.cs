using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Web.Services;
using Web.Core.Interfaces;
using Web.Infrastructure.Repositories;
using Web.Infrastructure.Logging;
using Web.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Repositories
builder.Services.AddScoped<IPlantRepository, PlantRepository>();
builder.Services.AddScoped<ISyncRepository, SyncRepository>();
builder.Services.AddScoped<IRemotePlantRepository, RemotePlantRepository>();
builder.Services.AddScoped<IReportingRepository, ReportingRepository>();
builder.Services.AddScoped<IBarcodePrintRepository, BarcodePrintRepository>();
builder.Services.AddScoped<IProductionOrderRepository, ProductionOrderRepository>();

// SyncService configuration and registration
// Plant configurations are now loaded dynamically from the PlantConfiguration table
// SyncService registration as Singleton with configuration values from appsettings.json
builder.Services.AddSingleton<SyncService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<SyncService>>();
    var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
    
    // Read configuration values from appsettings.json
    var syncIntervalSeconds = config.GetValue<int>("Sync:SyncIntervalSeconds", 30);
    var batchSize = config.GetValue<int>("Sync:BatchSize", 100);
    var matchWindowMinutes = config.GetValue<int>("Sync:MatchWindowMinutes", 60);
    
    return new SyncService(
        scopeFactory,
        logger,
        syncIntervalSeconds * 1000, // Convert seconds to milliseconds
        batchSize,
        matchWindowMinutes
    );
});

// ReportingService registration
builder.Services.AddScoped<ReportingService>();

// File Logging Service with 2-day retention
builder.Services.AddFileLogging(options =>
{
    options.LogDirectory = "Logs";
    options.RetentionDays = 2; // Keep only 2 days of logs
    options.MaxFileSizeMB = 10;
    options.EnableConsoleOutput = true;
});

// Background hosted service that starts/stops the sync loop
builder.Services.AddHostedService<SyncHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Logging Middleware - logs all requests and page visits
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionLoggingMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
