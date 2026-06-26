using Matrix.BuildingBlocks.Api.Defaults;
using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Matrix.Healthcare.Application;
using Matrix.Healthcare.Infrastructure;
using Matrix.Healthcare.Integration;

namespace Matrix.Healthcare.Api.Configurations
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
            services.AddHealthcareApplication();
            services.AddHealthcareInfrastructure(
                configuration: configuration,
                environment: builder.Environment,
                configureConsumers: consumers => consumers.AddHealthcareIntegrationConsumers());
        }
    }
}
