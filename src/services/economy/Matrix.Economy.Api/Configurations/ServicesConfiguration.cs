using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Api.Logging;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.Economy.Application;
using Matrix.Economy.Infrastructure;

namespace Matrix.Economy.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            IServiceCollection services = builder.Services;
            ConfigurationManager configuration = builder.Configuration;

            builder.AddSerilogLogging();

            builder.Services.AddControllers();

            services.AddJwtBearerAuthentication<InternalJwtOptions>(
                configuration: configuration,
                sectionName: InternalJwtOptions.SectionName);

            services.AddAuthorization();

            services.AddApplication();
            services.AddInfrastructure(
                configuration: configuration,
                environment: builder.Environment);
        }
    }
}
