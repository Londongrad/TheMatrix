using Matrix.Healthcare.Api.Configurations;
using Matrix.Healthcare.Infrastructure.Persistence;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

WebApplication app = builder.Build();

app.ConfigureApplicationMiddleware();
await app.Services.MigrateHealthcareDatabaseAsync();

app.Run();
