using Matrix.BuildingBlocks.Api.Defaults;
using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Matrix.Education.Application;
using Matrix.Education.Infrastructure;
using Matrix.Education.Integration;

namespace Matrix.Education.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            IServiceCollection services = builder.Services;
            ConfigurationManager configuration = builder.Configuration;

            builder.AddMatrixServiceDefaults();
            services.AddDatabaseStartup(configuration);
            services.AddMatrixInternalApi(configuration);
            services.AddEducationApplication();
            services.AddEducationInfrastructure(
                configuration: configuration,
                environment: builder.Environment,
                configureConsumers: consumers => consumers.AddEducationIntegrationConsumers());
        }
    }
}
