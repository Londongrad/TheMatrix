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
    }
}
