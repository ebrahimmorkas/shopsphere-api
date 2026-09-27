using ShopSphere.Application;
using ShopSphere.Infrastructure;
using ShopSphere.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.ApplyMigrationsAsync();
}

app.MapGet("/", () => "ShopSphere API");

await app.RunAsync();

public partial class Program;
