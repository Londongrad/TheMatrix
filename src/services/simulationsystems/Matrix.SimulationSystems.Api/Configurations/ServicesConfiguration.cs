using Matrix.BuildingBlocks.Api.Defaults;
using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Matrix.SimulationSystems.Application;
using Matrix.SimulationSystems.Infrastructure;

namespace Matrix.SimulationSystems.Api.Configurations
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
            services.AddInfrastructure(
                configuration: configuration,
                environment: builder.Environment);
        }
    }
}
