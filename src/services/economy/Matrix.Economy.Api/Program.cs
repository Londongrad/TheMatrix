using Matrix.Economy.Api.Configurations;
using Matrix.Economy.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

WebApplication app = builder.Build();

app.ConfigureApplicationMiddleware();
await app.Services.MigrateEconomyDatabaseAsync();

app.Run();
