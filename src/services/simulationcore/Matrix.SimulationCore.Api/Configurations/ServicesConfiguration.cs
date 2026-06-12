using Matrix.BuildingBlocks.Api.Defaults;
using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Matrix.SimulationCore.Application;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity;
using Matrix.SimulationCore.Infrastructure;
using Matrix.SimulationCore.Infrastructure.Scenarios.ClassicCity;

namespace Matrix.SimulationCore.Api.Configurations
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
            services.AddInfrastructure(configuration);
            services.AddClassicCityScenarioInfrastructure(configuration);
        }
    }
}
