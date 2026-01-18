using Matrix.ApiGateway.Authorization.AuthContext.Options;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.Authorization.InternalJwt.Abstractions;
using Matrix.ApiGateway.Authorization.PermissionsVersion;
using Matrix.BuildingBlocks.Api.Authorization;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Matrix.ApiGateway.Configurations.DependencyInjection
{
    public static class GatewayAuthServiceCollectionExtensions
    {
        public static IServiceCollection AddGatewayAuth(
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

            services.AddOptions<InternalJwtOptions>()
               .Bind(configuration.GetSection(InternalJwtOptions.SectionName))
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Issuer),
                    failureMessage: "InternalJwt:Issuer is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.Audience),
                    failureMessage: "InternalJwt:Audience is required.")
               .Validate(
                    validation: o => !string.IsNullOrWhiteSpace(o.SigningKey),
                    failureMessage: "InternalJwt:SigningKey is required.")
               .Validate(
                    validation: o => o.LifetimeSeconds > 0,
                    failureMessage: "InternalJwt:LifetimeSeconds must be > 0.")
               .ValidateOnStart();

            services.AddOptions<AuthContextOptions>()
               .Bind(configuration.GetSection(AuthContextOptions.SectionName))
               .Validate(
                    validation: o => o.CacheTtlSeconds > 0,
                    failureMessage: "AuthContext:CacheTtlSeconds must be greater than 0.")
               .ValidateOnStart();

            services.AddSingleton<IInternalJwtIssuer, InternalJwtIssuer>();

            return services;
        }
    }
}
