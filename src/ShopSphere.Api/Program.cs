using Scalar.AspNetCore;
using ShopSphere.Api;
using ShopSphere.Api.Extensions;
using ShopSphere.Application;
using ShopSphere.Infrastructure;
using ShopSphere.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation()
    .AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("ShopSphere API"));

    await app.Services.ApplyMigrationsAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapEndpoints();

await app.RunAsync();

public partial class Program;
