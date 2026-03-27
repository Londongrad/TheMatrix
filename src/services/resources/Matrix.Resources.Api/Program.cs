using Matrix.Resources.Api.Configurations;
using Matrix.Resources.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

WebApplication app = builder.Build();

app.ConfigureApplicationMiddleware();
await app.Services.MigrateResourcesDatabaseAsync();

app.Run();
