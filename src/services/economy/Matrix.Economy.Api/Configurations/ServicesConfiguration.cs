using Matrix.Economy.Application;
using Matrix.Economy.Infrastructure;

namespace Matrix.Economy.Api.Configurations
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
