using Application.Interface;
using Infrastructure.DBs;
using Infrastructure.Service;
using Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DBContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---- ADD CORS HERE ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder =>
        {
            builder.WithOrigins("http://localhost:3000") // React app
                   .AllowAnyHeader()
                   .AllowAnyMethod()
                   .AllowCredentials();
        });
});

// DI Services
builder.Services.AddScoped<IAssetHierarchyService, AssetHierarchyService>();
builder.Services.AddScoped<IAssetConfiguration, AssetConfigurationService>();
builder.Services.AddScoped<IMappingService, AssetMappingService>();

var app = builder.Build();

// Seeder
using (var scope = app.Services.CreateScope())
{
    var dbcontext = scope.ServiceProvider.GetRequiredService<DBContext>();
    await SignalTypessSeeder.SeedAsync(dbcontext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware order MATTERS
app.UseHttpsRedirection();

app.UseCors("AllowFrontend");  // <-- IMPORTANT: after HTTPS & before authorization

app.UseAuthorization();

app.MapControllers();

app.Run();
