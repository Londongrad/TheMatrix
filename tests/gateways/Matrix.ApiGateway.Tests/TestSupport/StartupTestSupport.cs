using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Matrix.ApiGateway.Tests.TestSupport;

public static class StartupTestSupport
{
    public static IConfiguration BuildValidApiConfiguration()
    {
        Dictionary<string, string?> values = new()
        {
            ["ExternalJwt:Issuer"] = "https://identity.test",
            ["ExternalJwt:Audience"] = "matrix-clients",
            ["ExternalJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
            ["InternalUserContextJwt:Issuer"] = "https://gateway.test",
            ["InternalUserContextJwt:Audience"] = "internal-gateway-clients",
            ["InternalUserContextJwt:SigningKey"] = "abcdef0123456789abcdef0123456789",
            ["InternalUserContextJwt:LifetimeSeconds"] = "300",
            ["IdentityInternal:BaseUrl"] = "https://identity.test",
            ["IdentityInternal:ApiKey"] = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&",
            ["IdentityInternal:RequestTimeoutSeconds"] = "15",
            ["PermissionsVersion:CacheTtlSeconds"] = "300",
            ["PermissionsVersion:StaleCacheTtlSeconds"] = "21600",
            ["PermissionsVersion:AllowStaleCacheOnIdentityFailure"] = "true",
            ["AuthContext:CacheTtlSeconds"] = "1800",
            ["CityOperationsDashboard:PanelReadTimeoutSeconds"] = "9",
            ["CityOperationsDashboard:HealthProbeTimeoutSeconds"] = "11",
            ["CityOperationsDashboard:MaxConcurrentCitySnapshotLoads"] = "7",
            ["FrontendSecurity:EnforceCookieOriginProtection"] = "true",
            ["FrontendSecurity:AllowedOrigins:0"] = "https://frontend.matrix.test",
            ["TrustedForwardedHeaders:Enabled"] = "true",
            ["TrustedForwardedHeaders:TrustLoopback"] = "true",
            ["TrustedForwardedHeaders:ForwardLimit"] = "2",
            ["DownstreamReadResilience:Enabled"] = "true",
            ["DownstreamReadResilience:MaxRetryAttempts"] = "2",
            ["DownstreamReadResilience:BaseRetryDelayMilliseconds"] = "200",
            ["DownstreamReadResilience:CircuitBreakerConsecutiveFailureThreshold"] = "5",
            ["DownstreamReadResilience:CircuitBreakDurationSeconds"] = "30",
            ["DownstreamServices:SimulationCore"] = "https://simulationcore.test",
            ["DownstreamServices:SimulationSystems"] = "https://simulationsystems.test",
            ["DownstreamServices:Economy"] = "https://economy.test",
            ["DownstreamServices:Resources"] = "https://resources.test",
            ["DownstreamServices:Population"] = "https://population.test",
            ["DownstreamServices:Identity"] = "https://identity.test",
            ["RabbitMq:Host"] = "rabbitmq.test",
            ["RabbitMq:Port"] = "5672",
            ["RabbitMq:VirtualHost"] = "/",
            ["RabbitMq:Username"] = "guest",
            ["RabbitMq:Password"] = "guest",
            ["Redis:ConnectionString"] = "localhost:6379",
            ["Redis:InstanceName"] = "matrix-tests:",
            ["ClassicCitySetupSessions:CacheTtlHours"] = "168",
            ["ClassicCitySetupSessions:DraftTtlMinutes"] = "60",
            ["ClassicCitySetupSessions:RecentDraftReuseWindowSeconds"] = "30",
            ["ClassicCitySetupSessions:MutationLockLeaseSeconds"] = "900",
            ["ClassicCitySetupSessions:MutationLockAcquireTimeoutMilliseconds"] = "1500",
            ["ClassicCitySetupSessions:MutationLockRetryDelayMilliseconds"] = "100",
            ["ClassicCitySetupSessions:ReconciliationEnabled"] = "true",
            ["ClassicCitySetupSessions:ReconciliationIntervalSeconds"] = "15",
            ["ClassicCitySetupSessions:LaunchQueueRecoveryDelaySeconds"] = "20"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    public static WebApplicationBuilder CreateBuilder(
        IConfiguration? configuration = null,
        string environmentName = "Development")
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = environmentName
        });

        if (configuration is not null)
        {
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddConfiguration(configuration);
        }

        return builder;
    }
}
