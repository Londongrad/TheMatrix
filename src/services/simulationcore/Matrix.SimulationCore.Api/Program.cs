using Matrix.SimulationCore.Api.Configurations;
using Matrix.SimulationCore.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

WebApplication app = builder.Build();

app.ConfigureApplicationMiddleware();

await app.MigrateSimulationCoreDatabaseAsync();

app.Run();
