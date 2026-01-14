using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Matrix.Population.Application;
using Matrix.Population.Infrastructure;

namespace Matrix.Population.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            IServiceCollection services = builder.Services;
            ConfigurationManager configuration = builder.Configuration;

            builder.Services.AddControllers();

            // JWT auth
            services.AddJwtBearerAuthentication<InternalJwtOptions>(
                configuration: configuration,
                sectionName: InternalJwtOptions.SectionName);

            services.AddAuthorization();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

            services.AddApplication();
            services.AddInfrastructure(configuration);
        }
    }
}
