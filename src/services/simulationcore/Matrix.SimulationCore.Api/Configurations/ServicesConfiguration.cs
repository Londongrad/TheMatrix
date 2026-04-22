using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Api.Defaults;
using Matrix.BuildingBlocks.Api.HealthChecks;
using Matrix.BuildingBlocks.Api.Logging;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Matrix.SimulationCore.Application;
using Matrix.SimulationCore.Infrastructure;

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
            services.AddInfrastructure(configuration);
        }
    }
}
