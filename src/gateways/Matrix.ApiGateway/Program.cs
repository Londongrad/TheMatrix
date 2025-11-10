using Matrix.ApiGateway.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureApplicationServices();

var app = builder.Build();

app.ConfigureApplicationMiddleware();
app.ConfigureApplicationEndpoints();

app.Run();
