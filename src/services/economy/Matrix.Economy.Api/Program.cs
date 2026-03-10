using Matrix.Economy.Api.Configurations;
using Matrix.Economy.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

var app = builder.Build();

app.ConfigureApplicationMiddleware();
await app.Services.MigrateEconomyDatabaseAsync();

app.Run();
