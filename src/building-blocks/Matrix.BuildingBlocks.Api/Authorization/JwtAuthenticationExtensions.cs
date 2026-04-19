using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Matrix.BuildingBlocks.Api.Authorization
{
    public static class JwtAuthenticationExtensions
    {
        public const string InternalCompositeJwtScheme = "InternalCompositeJwt";
        public const string InternalUserContextJwtScheme = "InternalUserContextJwt";
        public const string InternalServiceJwtScheme = "InternalServiceJwt";

        public static IServiceCollection AddJwtBearerAuthentication<TJwtOptions>(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName,
            bool requireHttpsMetadata = false,
            bool saveToken = false,
            Action<AuthenticationOptions>? configureAuthentication = null,
            Action<JwtBearerOptions>? configureJwtBearer = null)
            where TJwtOptions : class, IJwtValidationOptions
        {
            services.AddJwtValidationOptions<TJwtOptions>(
                configuration: configuration,
                sectionName: sectionName);

            if (configureAuthentication is null)
                services
                   .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                   .AddJwtBearer();
            else
                services
                   .AddAuthentication(configureAuthentication)
                   .AddJwtBearer();

            services.ConfigureJwtBearerOptions<TJwtOptions>(
                scheme: JwtBearerDefaults.AuthenticationScheme,
                requireHttpsMetadata: requireHttpsMetadata,
                saveToken: saveToken,
                configureJwtBearer: configureJwtBearer);

            return services;
        }

        public static IServiceCollection AddInternalJwtAuthentication(
            this IServiceCollection services,
            IConfiguration configuration,
            bool requireHttpsMetadata = false,
            bool saveToken = false)
        {
            services.AddJwtValidationOptions<InternalUserContextJwtOptions>(
                    configuration: configuration,
                    sectionName: InternalUserContextJwtOptions.SectionName,
                    legacySectionName: InternalJwtOptions.SectionName)
               .Validate(
                    validation: options => options.LifetimeSeconds > 0,
                    failureMessage: $"{InternalUserContextJwtOptions.SectionName}:LifetimeSeconds must be > 0.")
               .ValidateOnStart();

            services.AddJwtValidationOptions<InternalServiceJwtOptions>(
                    configuration: configuration,
                    sectionName: InternalServiceJwtOptions.SectionName,
                    legacySectionName: InternalJwtOptions.SectionName)
               .Validate(
                    validation: options => options.LifetimeSeconds > 0,
                    failureMessage: $"{InternalServiceJwtOptions.SectionName}:LifetimeSeconds must be > 0.")
               .ValidateOnStart();

            services
               .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = InternalCompositeJwtScheme;
                    options.DefaultChallengeScheme = InternalCompositeJwtScheme;
                })
               .AddPolicyScheme(
                    authenticationScheme: InternalCompositeJwtScheme,
                    displayName: InternalCompositeJwtScheme,
                    configureOptions: options =>
                    {
                        options.ForwardDefaultSelector = ResolveInternalJwtScheme;
                    })
               .AddJwtBearer(InternalUserContextJwtScheme)
               .AddJwtBearer(InternalServiceJwtScheme);

            services.ConfigureJwtBearerOptions<InternalUserContextJwtOptions>(
                scheme: InternalUserContextJwtScheme,
                requireHttpsMetadata: requireHttpsMetadata,
                saveToken: saveToken,
                configureJwtBearer: null);

            services.ConfigureJwtBearerOptions<InternalServiceJwtOptions>(
                scheme: InternalServiceJwtScheme,
                requireHttpsMetadata: requireHttpsMetadata,
                saveToken: saveToken,
                configureJwtBearer: null);

            return services;
        }

        public static OptionsBuilder<TJwtOptions> AddJwtValidationOptions<TJwtOptions>(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName,
            string? legacySectionName = null)
            where TJwtOptions : class, IJwtValidationOptions
        {
            var optionsBuilder = services.AddOptions<TJwtOptions>()
               .Configure(options =>
                {
                    IConfigurationSection primarySection = configuration.GetSection(sectionName);
                    IConfigurationSection? configuredSection =
                        HasConfiguredJwtValues(primarySection)
                            ? primarySection
                            : !string.IsNullOrWhiteSpace(legacySectionName)
                                ? configuration.GetSection(legacySectionName)
                                : null;

                    configuredSection?.Bind(options);
                })
               .Validate(
                    validation: options => !string.IsNullOrWhiteSpace(options.Issuer),
                    failureMessage: $"{sectionName}:Issuer is required.")
               .Validate(
                    validation: options => !string.IsNullOrWhiteSpace(options.Audience),
                    failureMessage: $"{sectionName}:Audience is required.")
               .Validate(
                    validation: options => !string.IsNullOrWhiteSpace(options.SigningKey),
                    failureMessage: $"{sectionName}:SigningKey is required.");

            if (RequiresInternalSigningKeyValidation(typeof(TJwtOptions)))
                optionsBuilder = optionsBuilder.Validate(
                    validation: options => InternalJwtSigningKeyPolicy.TryValidate(
                        signingKey: options.SigningKey,
                        validationError: out _),
                    failureMessage:
                    $"{sectionName}:SigningKey must be at least {InternalJwtSigningKeyPolicy.MinSigningKeyBytes} UTF-8 bytes long, contain at least {InternalJwtSigningKeyPolicy.MinDistinctCharacters} distinct characters, and avoid low-entropy secrets.");

            return optionsBuilder.ValidateOnStart();
        }

        private static void ConfigureJwtBearerOptions<TJwtOptions>(
            this IServiceCollection services,
            string scheme,
            bool requireHttpsMetadata,
            bool saveToken,
            Action<JwtBearerOptions>? configureJwtBearer)
            where TJwtOptions : class, IJwtValidationOptions
        {
            services.AddOptions<JwtBearerOptions>(scheme)
               .Configure<IOptions<TJwtOptions>>((jwtBearerOptions, jwtOptions) =>
                {
                    TJwtOptions jwt = jwtOptions.Value;

                    jwtBearerOptions.RequireHttpsMetadata = requireHttpsMetadata;
                    jwtBearerOptions.SaveToken = saveToken;
                    jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };

                    configureJwtBearer?.Invoke(jwtBearerOptions);
                });
        }

        private static string ResolveInternalJwtScheme(HttpContext context)
        {
            string? token = TryReadBearerToken(context);
            if (string.IsNullOrWhiteSpace(token))
                return InternalUserContextJwtScheme;

            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

                string? tokenKind = jwt.Claims.FirstOrDefault(
                        claim => claim.Type == JwtClaimNames.InternalTokenKind)
                   ?.Value;

                if (string.Equals(
                        a: tokenKind,
                        b: InternalJwtTokenKinds.Service,
                        comparisonType: StringComparison.Ordinal))
                    return InternalServiceJwtScheme;

                if (string.Equals(
                        a: tokenKind,
                        b: InternalJwtTokenKinds.UserContext,
                        comparisonType: StringComparison.Ordinal))
                    return InternalUserContextJwtScheme;

                return jwt.Claims.Any(claim => claim.Type == JwtClaimNames.Service)
                    ? InternalServiceJwtScheme
                    : InternalUserContextJwtScheme;
            }
            catch
            {
                return InternalUserContextJwtScheme;
            }
        }

        private static string? TryReadBearerToken(HttpContext context)
        {
            string? authorization = context.Request.Headers.Authorization;
            if (string.IsNullOrWhiteSpace(authorization) ||
                !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;

            return authorization["Bearer ".Length..].Trim();
        }

        private static bool HasConfiguredJwtValues(IConfigurationSection section)
        {
            return !string.IsNullOrWhiteSpace(section["Issuer"]) ||
                   !string.IsNullOrWhiteSpace(section["Audience"]) ||
                   !string.IsNullOrWhiteSpace(section["SigningKey"]);
        }

        private static bool RequiresInternalSigningKeyValidation(Type optionsType)
        {
            return optionsType == typeof(InternalJwtOptions) ||
                   optionsType == typeof(InternalUserContextJwtOptions) ||
                   optionsType == typeof(InternalServiceJwtOptions);
        }
    }
}
