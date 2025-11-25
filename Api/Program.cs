using Application.Interface;
using Infrastructure.DBs;
using Infrastructure.Service;
using Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// -------------------- Configuration --------------------
builder.Services.Configure<TelemetryOptions>(builder.Configuration.GetSection("Telemetry"));

var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");

// -------------------- DbContext / DbContextFactory --------------------
// Use AddDbContextFactory for background services and caching
builder.Services.AddDbContextFactory<DBContext>(options =>
    options.UseSqlServer(connectionString));

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

// -------------------- Singleton / Cache --------------------
builder.Services.AddSingleton<IMappingCache>(sp =>
{
    var dbFactory = sp.GetRequiredService<IDbContextFactory<DBContext>>();
    return new MappingCache(dbFactory); // default refresh interval 60s
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
    // Use factory to create a context for seeding
    using var dbContext = dbContextFactory.CreateDbContext();
    await SignalTypessSeeder.SeedAsync(dbContext);
}

// -------------------- Middleware --------------------
app.UseHttpsRedirection();

// Apply CORS
app.UseCors("AllowReactApp");

app.UseAuthorization();

app.MapControllers();

// -------------------- Run App --------------------
app.Run();
