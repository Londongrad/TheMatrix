using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.Population.Application;
using Matrix.Population.Infrastructure;

namespace Matrix.Population.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            IServiceCollection services = builder.Services;
            ConfigurationManager configuration = builder.Configuration;

            builder.Services.AddControllers();
            services.AddMatrixPermissionsPolicies();
            services.AddApplication();
            services.AddInfrastructure(configuration);
        }
    }
}
