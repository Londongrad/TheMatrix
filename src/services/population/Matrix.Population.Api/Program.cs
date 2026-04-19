using Matrix.Population.Api.Configurations;
using Matrix.Population.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

WebApplication app = builder.Build();

app.ConfigureApplicationMiddleware();

await app.Services.MigratePopulationDatabaseAsync();

app.Run();
