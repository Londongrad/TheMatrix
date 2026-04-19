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
                    validation: options => HasAnySigningKeyMaterial(options),
                    failureMessage: $"{sectionName}:SigningKey is required.");

            if (RequiresInternalSigningKeyValidation(typeof(TJwtOptions)))
                optionsBuilder = optionsBuilder.Validate(
                    validation: options => TryValidateInternalKeyMaterial(
                        options: options,
                        optionsPath: sectionName),
                    failureMessage: $"{sectionName}: invalid signing key material.");

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
                    var tokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };

                    if (jwt is IInternalJwtKeyRingOptions keyRingOptions)
                    {
                        InternalJwtResolvedKeyRing keyRing = InternalJwtKeyRingPolicy.Resolve(
                            options: keyRingOptions,
                            optionsPath: scheme);

                        tokenValidationParameters.IssuerSigningKeyResolver = (
                            token,
                            securityToken,
                            kid,
                            validationParameters) => ResolveIssuerSigningKeys(
                            keyRing: keyRing,
                            kid: kid);
                    }
                    else
                    {
                        tokenValidationParameters.IssuerSigningKey =
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
                    }

                    jwtBearerOptions.TokenValidationParameters = tokenValidationParameters;

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
                   !string.IsNullOrWhiteSpace(section["SigningKey"]) ||
                   !string.IsNullOrWhiteSpace(section["CurrentKeyId"]) ||
                   section.GetSection("Keys")
                      .GetChildren()
                      .Any();
        }

        private static bool RequiresInternalSigningKeyValidation(Type optionsType)
        {
            return optionsType == typeof(InternalJwtOptions) ||
                   optionsType == typeof(InternalUserContextJwtOptions) ||
                   optionsType == typeof(InternalServiceJwtOptions);
        }

        private static bool HasAnySigningKeyMaterial<TJwtOptions>(TJwtOptions options)
            where TJwtOptions : class, IJwtValidationOptions
        {
            if (!string.IsNullOrWhiteSpace(options.SigningKey))
                return true;

            return options is IInternalJwtKeyRingOptions
            {
                Keys.Count: > 0
            };
        }

        private static bool TryValidateInternalKeyMaterial<TJwtOptions>(
            TJwtOptions options,
            string optionsPath)
            where TJwtOptions : class, IJwtValidationOptions
        {
            try
            {
                if (options is IInternalJwtKeyRingOptions keyRingOptions)
                {
                    _ = InternalJwtKeyRingPolicy.Resolve(
                        options: keyRingOptions,
                        optionsPath: optionsPath);

                    return true;
                }

                return InternalJwtSigningKeyPolicy.TryValidate(
                    signingKey: options.SigningKey,
                    validationError: out _);
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<SecurityKey> ResolveIssuerSigningKeys(
            InternalJwtResolvedKeyRing keyRing,
            string? kid)
        {
            if (string.IsNullOrWhiteSpace(kid))
                return keyRing.Keys.Values
                   .Select(CreateSecurityKey)
                   .ToArray();

            if (!keyRing.Keys.TryGetValue(
                    key: kid,
                    value: out string? signingKey))
                return [];

            return [CreateSecurityKey(signingKey)];
        }

        private static SecurityKey CreateSecurityKey(string signingKey)
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        }
    }
}
