using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Api.Defaults;
using Matrix.BuildingBlocks.Api.Forwarding;
using Matrix.BuildingBlocks.Api.OptionsValidation;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Infrastructure.DatabaseStartup;
using Matrix.Identity.Application;
using Matrix.Identity.Infrastructure;
using Matrix.Identity.Infrastructure.Authentication.ExternalJwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Matrix.Identity.Api.Configurations
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this WebApplicationBuilder builder)
        {
            IServiceCollection services = builder.Services;
            ConfigurationManager configuration = builder.Configuration;

            builder.AddMatrixServiceDefaults();
            services.AddTrustedForwardedHeaders(configuration);
            services.AddDatabaseStartup(configuration);

            services
               .AddApplicationLayer()
               .AddInfrastructureLayer(configuration)
               .AddSecurityLayer(configuration);
        }

        private static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            services.AddApplication();
            return services;
        }

        private static IServiceCollection AddInfrastructureLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddInfrastructure(configuration);
            return services;
        }

        private static IServiceCollection AddSecurityLayer(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddJwtBearerAuthentication<ExternalJwtOptions>(
                configuration: configuration,
                sectionName: ExternalJwtOptions.SectionName,
                requireHttpsMetadata: false,
                configureAuthentication: options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                });

            services.AddOptions<IdentityInternalOptions>()
               .BindConfiguration(IdentityInternalOptions.SectionName)
               .ValidateInternalApiKeyRing(IdentityInternalOptions.SectionName)
               .ValidateOnStart();

            services.AddAuthorization();
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

            return services;
        }
    }
}
