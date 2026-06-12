using Matrix.BuildingBlocks.Api.Defaults;
using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Matrix.Resources.Application;
using Matrix.Resources.Application.Scenarios.ClassicCity;
using Matrix.Resources.Infrastructure;
using Matrix.Resources.Infrastructure.Scenarios.ClassicCity;

namespace Matrix.Resources.Api.Configurations
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

            services.AddApplication();
            services.AddClassicCityScenarioApplication();
            services.AddInfrastructure(
                configuration: configuration,
                environment: builder.Environment,
                configureConsumers: consumers => consumers.AddClassicCityScenarioConsumers());
            services.AddClassicCityScenarioInfrastructure();
        }
    }
}
