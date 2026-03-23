using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Api.HealthChecks;
using Matrix.BuildingBlocks.Api.Logging;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
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

            builder.AddSerilogLogging();

            services.AddControllers();
            services.AddOperationalHealthChecks(configuration);

            services.AddJwtBearerAuthentication<InternalJwtOptions>(
                configuration: configuration,
                sectionName: InternalJwtOptions.SectionName);

            services.AddAuthorization();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

            services.AddApplication();
            services.AddInfrastructure(
                configuration: configuration,
                environment: builder.Environment);
        }
    }
}
