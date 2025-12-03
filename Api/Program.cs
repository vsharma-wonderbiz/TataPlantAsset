using Api.Extesnion; // your extension methods
using Application.Interface;
using Infrastructure.Configuration;
using Infrastructure.DBs;
using Infrastructure.Hubs;
using Infrastructure.Seeding;
using Infrastructure.Service;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Serilog --------------------
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// Let the Host use Serilog for the built-in logging pipeline
builder.Host.UseSerilog();

// -------------------- Configuration --------------------
builder.Services.Configure<TelemetryOptions>(builder.Configuration.GetSection("Telemetry"));
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

// -------------------- DbContextFactory --------------------
builder.Services.AddDbContextFactory<DBContext>(options =>
    options.UseSqlServer(connectionString));

// -------------------- Authentication & Authorization --------------------
// This extension should add authentication schemes (e.g., JWT) and configure JwtBearerOptions if needed.
builder.Services.AddCustomAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// -------------------- Controllers / Swagger --------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -------------------- CORS --------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policyBuilder =>
    {
        policyBuilder
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// -------------------- Options + Scoped Services --------------------
builder.Services.Configure<InfluxDbOptions>(configuration.GetSection("InfluxDb"));
// TelemetryOptions already configured above — don't repeat it.

builder.Services.AddSingleton<IInfluxDbConnectionService, InfluxDbConnectionService>();
builder.Services.AddScoped<IInfluxTelementryService, InfluxTelemetryService>();

builder.Services.AddScoped<IAssetHierarchyService, AssetHierarchyService>();
builder.Services.AddScoped<IAssetConfiguration, AssetConfigurationService>();
builder.Services.AddScoped<IMappingService, AssetMappingService>();

// Notification service + cleanup
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<ExpiredNotificationCleanupService>();

// -------------------- SignalR --------------------
builder.Services.AddSignalR();

// Register IUserIdProvider so SignalR Clients.User(...) maps correctly to your stored user id
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, Infrastructure.Hubs.NameIdentifierUserIdProvider>();

// -------------------- Singleton / Cache --------------------
builder.Services.AddSingleton<IMappingCache>(sp =>
{
    var dbFactory = sp.GetRequiredService<IDbContextFactory<DBContext>>();
    return new MappingCache(dbFactory);
});

// -------------------- Background Services --------------------
builder.Services.AddHostedService<InfluxDbInitializationService>();
builder.Services.AddHostedService<TelemetryBackgroundService>();

builder.Services.AddSingleton<IAlertStateStore, MemoryAlertStateStore>();


// -------------------- Build App --------------------
var app = builder.Build();

// -------------------- Serilog request logging --------------------
app.UseSerilogRequestLogging();

// -------------------- Swagger UI --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// -------------------- Seeder Execution --------------------
using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DBContext>>();
    using var dbContext = dbContextFactory.CreateDbContext();
    await SignalTypessSeeder.SeedAsync(dbContext);
}

// -------------------- Map Hub --------------------
app.MapHub<NotificationHub>("/hubs/notifications");

// -------------------- Middleware (CORRECT ORDER) --------------------
app.UseHttpsRedirection();
app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// -------------------- Run App --------------------
app.Run();
