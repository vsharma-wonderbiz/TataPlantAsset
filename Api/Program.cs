using Application.Interface;
using Infrastructure.DBs;
using Infrastructure.Service;
using Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Api.Extesnion; // ⭐ ADD THIS - Import your extension namespace
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//--------------------------Serliog-------------------------------

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// -------------------- Configuration --------------------
builder.Services.Configure<TelemetryOptions>(builder.Configuration.GetSection("Telemetry"));
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");

// -------------------- DbContext / DbContextFactory --------------------
builder.Services.AddDbContextFactory<DBContext>(options =>
    options.UseSqlServer(connectionString));

// -------------------- Authentication & Authorization -------------------- ⭐ ADD THIS
builder.Services.AddCustomAuthentication(builder.Configuration);

// -------------------- Controllers --------------------
builder.Services.AddControllers();

// -------------------- Swagger / OpenAPI --------------------
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

// -------------------- Scoped Services --------------------
builder.Services.AddScoped<IAssetHierarchyService, AssetHierarchyService>();
builder.Services.AddScoped<IAssetConfiguration, AssetConfigurationService>();
builder.Services.AddScoped<IMappingService, AssetMappingService>();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// -------------------- Singleton / Cache --------------------
builder.Services.AddSingleton<IMappingCache>(sp =>
{
    var dbFactory = sp.GetRequiredService<IDbContextFactory<DBContext>>();
    return new MappingCache(dbFactory);
});

// -------------------- Background Services --------------------
builder.Services.AddHostedService<TelemetryBackgroundService>();

// -------------------- Build App --------------------
var app = builder.Build();

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

// -------------------- Middleware (CORRECT ORDER) --------------------
app.UseHttpsRedirection();
app.UseCors("AllowReactApp");

app.UseAuthentication();  // ⭐ ADD THIS - Must come BEFORE UseAuthorization
app.UseAuthorization();

app.MapControllers();

// -------------------- Run App --------------------
app.Run();