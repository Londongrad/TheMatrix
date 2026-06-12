using Matrix.BuildingBlocks.Api.Defaults;
using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Matrix.Economy.Application;
using Matrix.Economy.Application.Scenarios.ClassicCity;
using Matrix.Economy.Infrastructure;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity;

namespace Matrix.Economy.Api.Configurations
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
                environment: builder.Environment,
                configureConsumers: consumers => consumers.AddClassicCityScenarioConsumers());
            services.AddClassicCityScenarioInfrastructure();
        }
    }
}
