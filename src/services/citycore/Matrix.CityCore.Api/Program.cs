using Matrix.CityCore.Api.Configurations;
using Matrix.CityCore.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

WebApplication app = builder.Build();

app.ConfigureApplicationMiddleware();

await app.MigrateCityCoreDatabaseAsync();

app.Run();
