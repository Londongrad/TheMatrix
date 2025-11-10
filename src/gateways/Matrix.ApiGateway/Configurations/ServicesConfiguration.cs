using Matrix.ApiGateway.DownstreamClients.CityCore;
using Matrix.ApiGateway.DownstreamClients.Economy;
using Matrix.ApiGateway.DownstreamClients.Population;

namespace Matrix.ApiGateway.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            var services = builder.Services;
            var configuration = builder.Configuration;

            // Controllers
            services.AddControllers();

            // CORS frontend policy 
            services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy
                        .WithOrigins("http://localhost:5173")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // HttpClients for downstream services
            var downstream = configuration.GetSection("DownstreamServices");

            services.AddHttpClient<ICityCoreApiClient, CityCoreApiClient> (client =>
            {
                client.BaseAddress = new Uri(downstream["CityCore"]!);
            });

            services.AddHttpClient<IEconomyApiClient, EconomyApiClient>(client =>
            {
                client.BaseAddress = new Uri(downstream["Economy"]!);
            });

            services.AddHttpClient<IPopulationApiClient, PopulationApiClient>(client =>
            {
                client.BaseAddress = new Uri(downstream["Population"]!);
            });
        }
    }
}
