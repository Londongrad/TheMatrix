using Matrix.ApiGateway.Authorization.AuthContext.Options;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.Authorization.InternalJwt.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion;
using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Matrix.ApiGateway.Configurations.DependencyInjection
{
    public static class GatewayAuthServiceCollectionExtensions
    {
        public static IServiceCollection AddGatewayAuth(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.TryAddSingleton(TimeProvider.System);

            services.AddJwtBearerAuthentication<ExternalJwtOptions>(
                configuration: configuration,
                sectionName: ExternalJwtOptions.SectionName,
                requireHttpsMetadata: false,
                configureAuthentication: options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                },
                configureJwtBearer: options =>
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = ExternalJwtPermissionsVersionEvents.HandleTokenValidated,
                        OnChallenge = ExternalJwtPermissionsVersionEvents.HandleChallenge
                    };
                });

            services.AddAuthorization();

            services.AddJwtValidationOptions<InternalUserContextJwtOptions>(
                    configuration: configuration,
                    sectionName: InternalUserContextJwtOptions.SectionName)
               .Validate(
                    validation: o => o.LifetimeSeconds > 0,
                    failureMessage: $"{InternalUserContextJwtOptions.SectionName}:LifetimeSeconds must be > 0.")
               .ValidateOnStart();

            services.AddOptions<AuthContextOptions>()
               .Bind(configuration.GetSection(AuthContextOptions.SectionName))
               .Validate(
                    validation: o => o.CacheTtlSeconds > 0,
                    failureMessage: "AuthContext:CacheTtlSeconds must be greater than 0.")
               .ValidateOnStart();

            services.AddSingleton<IInternalJwtIssuer, InternalJwtIssuer>();
            services.AddSingleton<IInternalJwtRequestContextAccessor, InternalJwtRequestContextAccessor>();

            return services;
        }
    }
}
