using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Abstractions;
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

            services
               .AddPresentationLayer() // Controllers + Swagger
               .AddApplicationLayer() // MediatR, Application
               .AddInfrastructureLayer(configuration) // DbContext, репы, ExternalJwtAccessTokenService, PasswordHasher
               .AddSecurityLayer(configuration); // Authentication + Authorization
        }

        private static IServiceCollection AddPresentationLayer(this IServiceCollection services)
        {
            services.AddControllers();

            services.AddEndpointsApiExplorer();

            return services;
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
                saveToken: true,
                configureAuthentication: options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                });

            services.AddOptions<IdentityInternalOptions>()
               .BindConfiguration(IdentityInternalOptions.SectionName)
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.ApiKey),
                    failureMessage: "IdentityInternal:ApiKey is required.")
               .ValidateOnStart();

            services.AddAuthorization();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

            return services;
        }
    }
}
