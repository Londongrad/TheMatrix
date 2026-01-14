using System.Text;
using Matrix.CityCore.Application;
using Matrix.CityCore.Infrastructure;
using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;

namespace Matrix.CityCore.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            var services = builder.Services;
            var configuration = builder.Configuration;

            builder.Services.AddControllers();

            services.AddJwtBearerAuthentication<InternalJwtOptions>(
                configuration,
                InternalJwtOptions.SectionName);

            services.AddAuthorization();

            services.AddApplication();
            services.AddInfrastructure(configuration);
        }
    }
}
