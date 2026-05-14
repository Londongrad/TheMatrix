using Matrix.Identity.Api.Configurations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Matrix.Identity.Api.Tests.TestSupport;

public static class StartupTestSupport
{
    public static IConfiguration BuildValidApiConfiguration()
    {
        Dictionary<string, string?> values = new()
        {
            ["ConnectionStrings:IdentityDb"] = "Host=localhost;Port=5432;Database=identity_tests;Username=postgres;Password=postgres",
            ["ExternalJwt:Issuer"] = "https://identity.test",
            ["ExternalJwt:Audience"] = "matrix-clients",
            ["ExternalJwt:SigningKey"] = "0123456789abcdef0123456789abcdef",
            ["ExternalJwt:AccessTokenLifetimeMinutes"] = "30",
            ["ExternalJwt:RefreshTokenLifetimeDays"] = "7",
            ["ExternalJwt:ShortRefreshTokenLifetimeHours"] = "8",
            ["IdentityInternal:ApiKey"] = "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&",
            ["TrustedForwardedHeaders:Enabled"] = "true",
            ["TrustedForwardedHeaders:TrustLoopback"] = "true",
            ["TrustedForwardedHeaders:ForwardLimit"] = "2",
            ["RefreshTokenCleanup:PollIntervalSeconds"] = "300",
            ["RefreshTokenCleanup:BatchSize"] = "500",
            ["RefreshTokenCleanup:RevokedRetentionHours"] = "24",
            ["RefreshTokenCleanup:ExpiredRetentionHours"] = "24",
            ["OneTimeTokens:EmailConfirmationLifetimeMinutes"] = "1440",
            ["OneTimeTokens:EmailConfirmationCooldownSeconds"] = "60",
            ["OneTimeTokens:EmailConfirmationMaxDeliveryAttemptsPerHour"] = "5",
            ["OneTimeTokens:PasswordResetLifetimeMinutes"] = "60",
            ["OneTimeTokens:PasswordResetCooldownSeconds"] = "60",
            ["OneTimeTokens:PasswordResetMaxDeliveryAttemptsPerHour"] = "5",
            ["SecurityAudit:FailedLoginWindowMinutes"] = "15",
            ["SecurityAudit:FailedLoginMaxAttemptsPerLogin"] = "10",
            ["SecurityAudit:FailedLoginMaxAttemptsPerIp"] = "20",
            ["SecurityAudit:EmailConfirmationRequestWindowMinutes"] = "60",
            ["SecurityAudit:EmailConfirmationRequestMaxAttemptsPerEmail"] = "5",
            ["SecurityAudit:EmailConfirmationRequestMaxAttemptsPerIp"] = "20",
            ["SecurityAudit:PasswordResetRequestWindowMinutes"] = "60",
            ["SecurityAudit:PasswordResetRequestMaxAttemptsPerEmail"] = "5",
            ["SecurityAudit:PasswordResetRequestMaxAttemptsPerIp"] = "20",
            ["SecurityAuditCleanup:PollIntervalSeconds"] = "3600",
            ["SecurityAuditCleanup:BatchSize"] = "1000",
            ["SecurityAuditCleanup:RetentionDays"] = "30",
            ["Email:DeliveryMode"] = "LogOnly",
            ["FrontendLinks:BaseUrl"] = "https://frontend.test",
            ["FrontendLinks:ConfirmEmailPath"] = "/confirm-email",
            ["FrontendLinks:ResetPasswordPath"] = "/reset-password",
            ["GeoLocation:Enabled"] = "false",
            ["GeoLocation:TimeoutSeconds"] = "10",
            ["RabbitMq:Host"] = "rabbitmq.test",
            ["RabbitMq:Port"] = "5672",
            ["RabbitMq:VirtualHost"] = "/",
            ["RabbitMq:Username"] = "guest",
            ["RabbitMq:Password"] = "guest",
            ["DatabaseStartup:Enabled"] = "false"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    public static WebApplicationBuilder CreateBuilder(IConfiguration? configuration = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development"
        });

        if (configuration is not null)
        {
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddConfiguration(configuration);
        }

        return builder;
    }
}
