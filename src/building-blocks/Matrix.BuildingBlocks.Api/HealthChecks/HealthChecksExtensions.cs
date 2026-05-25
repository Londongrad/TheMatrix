using Matrix.BuildingBlocks.Application.Authorization.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Matrix.BuildingBlocks.Api.HealthChecks
{
    public static class HealthChecksExtensions
    {
        public static IServiceCollection AddOperationalHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<RabbitMqHealthCheckOptions>()
               .Bind(configuration.GetSection(RabbitMqHealthCheckOptions.SectionName))
               .Validate(
                    validation: o => o.TimeoutSeconds > 0,
                    failureMessage: "HealthChecks:RabbitMq:TimeoutSeconds must be greater than 0.")
               .ValidateOnStart();

            IHealthChecksBuilder builder = services.AddHealthChecks()
               .AddCheck(
                    name: "self",
                    check: () => HealthCheckResult.Healthy(),
                    tags: ["live"]);

            RabbitMqHealthCheckOptions options = configuration
                                                    .GetSection(RabbitMqHealthCheckOptions.SectionName)
                                                    .Get<RabbitMqHealthCheckOptions>() ??
                                                 new RabbitMqHealthCheckOptions();

            string? host = configuration["RabbitMq:Host"];
            ushort port = configuration.GetValue<ushort?>("RabbitMq:Port") ?? 5672;

            if (options.Enabled &&
                !string.IsNullOrWhiteSpace(host))
                builder.AddCheck(
                    name: "rabbitmq",
                    instance: new RabbitMqConnectivityHealthCheck(
                        host: host,
                        port: port,
                        timeout: TimeSpan.FromSeconds(options.TimeoutSeconds)),
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["ready"]);

            AddInternalJwtRotationHealthCheck<InternalUserContextJwtOptions>(
                builder: builder,
                configuration: configuration,
                sectionName: InternalUserContextJwtOptions.SectionName,
                displayName: "internal-user-context-jwt");

            AddInternalJwtRotationHealthCheck<InternalServiceJwtOptions>(
                builder: builder,
                configuration: configuration,
                sectionName: InternalServiceJwtOptions.SectionName,
                displayName: "internal-service-jwt");

            return services;
        }

        public static WebApplication MapOperationalHealthChecks(this WebApplication app)
        {
            app.MapHealthChecks(
                pattern: "/health/live",
                options: new HealthCheckOptions
                {
                    Predicate = registration => registration.Tags.Contains("live")
                });

            app.MapHealthChecks(
                pattern: "/health/ready",
                options: new HealthCheckOptions
                {
                    Predicate = registration => registration.Tags.Contains("ready")
                });

            return app;
        }

        private static void AddInternalJwtRotationHealthCheck<TOptions>(
            IHealthChecksBuilder builder,
            IConfiguration configuration,
            string sectionName,
            string displayName)
            where TOptions : class, IInternalJwtKeyRingOptions
        {
            TOptions? options = TryBindJwtOptions<TOptions>(
                configuration: configuration,
                sectionName: sectionName);

            if (options is null)
                return;

            (HealthStatus status, string description) = EvaluateInternalJwtRotationReadiness(
                options: options,
                sectionName: sectionName);

            builder.AddCheck(
                name: displayName,
                instance: new InternalJwtRotationHealthCheck(
                    name: displayName,
                    status: status,
                    description: description),
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready"]);
        }

        private static TOptions? TryBindJwtOptions<TOptions>(
            IConfiguration configuration,
            string sectionName)
            where TOptions : class
        {
            IConfigurationSection configuredSection = configuration.GetSection(sectionName);
            if (!HasConfiguredJwtValues(configuredSection))
                return null;

            return configuredSection.Get<TOptions>();
        }

        private static (HealthStatus Status, string Description) EvaluateInternalJwtRotationReadiness(
            IInternalJwtKeyRingOptions options,
            string sectionName)
        {
            try
            {
                InternalJwtResolvedKeyRing keyRing = InternalJwtKeyRingPolicy.Resolve(
                    options: options,
                    optionsPath: sectionName);

                if (string.Equals(
                        a: keyRing.CurrentKeyId,
                        b: InternalJwtKeyRingPolicy.LegacyKeyId,
                        comparisonType: StringComparison.Ordinal))
                    return (
                        HealthStatus.Degraded,
                        "using legacy single-key configuration; migrate to CurrentKeyId + Keys before rotating.");

                if (keyRing.Keys.Count < 2)
                    return (
                        HealthStatus.Degraded,
                        $"current key '{keyRing.CurrentKeyId}' is configured without overlap keys; add a secondary key before rotation.");

                return (
                    HealthStatus.Healthy,
                    $"current key '{keyRing.CurrentKeyId}' is active and {keyRing.Keys.Count} keys are available for overlap rotation.");
            }
            catch (Exception ex)
            {
                return (
                    HealthStatus.Unhealthy,
                    $"invalid key rotation configuration: {ex.Message}");
            }
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
    }
}
