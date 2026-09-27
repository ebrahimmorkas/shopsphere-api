using Scalar.AspNetCore;
using Serilog;
using ShopSphere.Api;
using ShopSphere.Api.Extensions;
using ShopSphere.Application;
using ShopSphere.Infrastructure;
using ShopSphere.Infrastructure.Authentication;
using ShopSphere.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation()
    .AddRateLimiting(builder.Configuration)
    .AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("ShopSphere API"));
}

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await app.Services.ApplyMigrationsAsync();
    await app.Services.SeedAdminAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseSerilogRequestLogging();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthCheckEndpoints();
app.MapEndpoints();

await app.RunAsync();

public partial class Program;
