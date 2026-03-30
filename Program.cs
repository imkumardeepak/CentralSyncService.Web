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



// ReportingService registration
builder.Services.AddScoped<ReportingService>();

// Excel Export Service registration
builder.Services.AddScoped<ExcelExportService>();

// File Logging Service with 2-day retention
builder.Services.AddFileLogging(options =>
{
    options.LogDirectory = "Logs";
    options.RetentionDays = 2; // Keep only 2 days of logs
    options.MaxFileSizeMB = 10;
    options.EnableConsoleOutput = true;
});



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
