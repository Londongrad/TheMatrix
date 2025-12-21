using Matrix.Identity.Api.Configurations;
using Matrix.Identity.Infrastructure.Persistence.Seed;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

WebApplication app = builder.Build();

app.ConfigureApplicationMiddleware();

await app.SeedIdentityPermissionsAsync();

app.Run();
