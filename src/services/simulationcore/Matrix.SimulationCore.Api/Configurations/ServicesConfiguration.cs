using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Api.HealthChecks;
using Matrix.BuildingBlocks.Api.Logging;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
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

            builder.AddSerilogLogging();

            builder.Services.AddControllers();
            builder.Services.AddOperationalHealthChecks(configuration);

            services.AddInternalJwtAuthentication(configuration);

            services.AddAuthorization();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

            services.AddApplication();
            services.AddInfrastructure(configuration);
        }
    }
}
