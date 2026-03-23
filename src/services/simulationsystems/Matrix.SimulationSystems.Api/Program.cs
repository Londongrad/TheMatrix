using Matrix.SimulationSystems.Api.Configurations;
using Matrix.SimulationSystems.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

WebApplication app = builder.Build();

app.ConfigureApplicationMiddleware();
await app.Services.MigrateSimulationSystemsDatabaseAsync();

app.Run();
