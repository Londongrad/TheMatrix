using Matrix.CityCore.Api.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

var app = builder.Build();

app.ConfigureApplicationMiddleware();

app.Run();