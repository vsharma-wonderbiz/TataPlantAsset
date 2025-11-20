using Application.Interface;
using Infrastructure.DBs;
using Infrastructure.Service;
using Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DBContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IAssetHierarchyService, AssetHierarchyService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//SignalType Seeder Class 
/*using (var cope = app.Services.CreateScope())
{
    var dbcontext = cope.ServiceProvider.GetRequiredService<DBContext>();
    await SignalTypessSeeder.SeedAsync(dbcontext);//these is the function that seeds the value 
} */

    app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
