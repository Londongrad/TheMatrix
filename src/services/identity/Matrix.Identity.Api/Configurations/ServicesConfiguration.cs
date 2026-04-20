using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Api.Forwarding;
using Matrix.BuildingBlocks.Api.HealthChecks;
using Matrix.BuildingBlocks.Api.Logging;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Security.InternalApiKey;
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

            builder.AddSerilogLogging();
            services.AddOperationalHealthChecks(configuration);
            services.AddTrustedForwardedHeaders(configuration);
            services.AddDatabaseStartup(configuration);

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
                configureAuthentication: options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                });

            services.AddOptions<IdentityInternalOptions>()
               .BindConfiguration(IdentityInternalOptions.SectionName)
               .Validate(
                    validation: o => TryValidateInternalApiKeyRing(
                        options: o,
                        validationError: out _),
                    failureMessage: $"{IdentityInternalOptions.SectionName}: invalid key rotation configuration.")
               .ValidateOnStart();

            services.AddAuthorization();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();

            return services;
        }

        private static bool TryValidateInternalApiKeyRing(
            IdentityInternalOptions options,
            out string? validationError)
        {
            try
            {
                _ = InternalApiKeyRingPolicy.Resolve(
                    options: options,
                    optionsPath: IdentityInternalOptions.SectionName);
                validationError = null;
                return true;
            }
            catch (Exception ex)
            {
                validationError = ex.Message;
                return false;
            }
        }
    }
}
