using Matrix.CityCore.Application;
using Matrix.CityCore.Infrastructure;

namespace Matrix.CityCore.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            var services = builder.Services;
            var configuration = builder.Configuration;

            builder.Services.AddControllers();

            services.AddApplication();
            services.AddInfrastructure(configuration);
        }
    }
}
