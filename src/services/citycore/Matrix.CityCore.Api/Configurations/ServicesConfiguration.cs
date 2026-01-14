using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.CityCore.Application;
using Matrix.CityCore.Infrastructure;

namespace Matrix.CityCore.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            IServiceCollection services = builder.Services;
            ConfigurationManager configuration = builder.Configuration;

            builder.Services.AddControllers();

            services.AddJwtBearerAuthentication<InternalJwtOptions>(
                configuration: configuration,
                sectionName: InternalJwtOptions.SectionName);

            services.AddAuthorization();

            services.AddApplication();
            services.AddInfrastructure(configuration);
        }
    }
}
