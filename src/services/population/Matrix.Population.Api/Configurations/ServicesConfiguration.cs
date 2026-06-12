using Matrix.BuildingBlocks.Api.Defaults;
using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Matrix.Population.Application;
using Matrix.Population.Application.Scenarios.ClassicCity;
using Matrix.Population.Infrastructure;
using Matrix.Population.Infrastructure.Scenarios.ClassicCity;

namespace Matrix.Population.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            IServiceCollection services = builder.Services;
            ConfigurationManager configuration = builder.Configuration;

            builder.AddMatrixServiceDefaults();
            builder.Services.AddDatabaseStartup(configuration);
            services.AddMatrixInternalApi(configuration);

            services.AddApplication();
            services.AddClassicCityScenarioApplication();
            services.AddInfrastructure(
                configuration: configuration,
                configureConsumers: consumers => consumers.AddClassicCityScenarioConsumers());
            services.AddClassicCityScenarioInfrastructure();
        }
    }
}
