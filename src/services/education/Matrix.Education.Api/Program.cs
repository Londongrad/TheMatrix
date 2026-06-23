using Matrix.Education.Api.Configurations;
using Matrix.Education.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

WebApplication app = builder.Build();

app.ConfigureApplicationMiddleware();
await app.Services.MigrateEducationDatabaseAsync();

app.Run();
