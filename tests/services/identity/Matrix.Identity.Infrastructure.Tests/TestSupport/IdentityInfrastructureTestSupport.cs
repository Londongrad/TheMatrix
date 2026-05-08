using System.Net;
using System.Text;
using System.Text.Json;
using Matrix.Identity.Application.Abstractions.Persistence;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Matrix.Identity.Domain.ValueObjects;
using Matrix.Identity.Infrastructure.Authentication.ExternalJwt;
using Matrix.Identity.Infrastructure.Integration.GeoLocation;
using Matrix.Identity.Infrastructure.Integration.Links;
using Matrix.Identity.Infrastructure.Persistence;
using Matrix.Identity.Infrastructure.Persistence.Models;
using Matrix.Identity.Infrastructure.Security.Audit;
using Matrix.Identity.Infrastructure.Security.Tokens;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DomainGeoLocation = Matrix.Identity.Domain.ValueObjects.GeoLocation;

namespace Matrix.Identity.Infrastructure.Tests.TestSupport;

internal static class IdentityInfrastructureTestSupport
{
    internal static readonly DateTime CreatedAtUtc = new(2048, 5, 1, 8, 0, 0, DateTimeKind.Utc);
    internal static readonly DateTime LaterUtc = CreatedAtUtc.AddHours(2);

    internal static IdentityTestDatabase CreateDbContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new IdentityDbContext(options);
        dbContext.Database.EnsureCreated();
        return new IdentityTestDatabase(dbContext, connection);
    }

    internal static FrozenClock CreateClock(DateTime? utcNow = null)
    {
        return new FrozenClock(utcNow ?? CreatedAtUtc);
    }

    internal static FrozenTimeProvider CreateTimeProvider(DateTimeOffset? utcNow = null)
    {
        return new FrozenTimeProvider(utcNow ?? new DateTimeOffset(CreatedAtUtc, TimeSpan.Zero));
    }

    internal static User CreateUser(
        string email = "neo@matrix.local",
        string username = "neo",
        DateTime? createdAtUtc = null)
    {
        return User.CreateNew(
            email: Email.Create(email),
            username: Username.Create(username),
            passwordHash: "hashed-password",
            createdAtUtc: createdAtUtc ?? CreatedAtUtc);
    }

    internal static UserSession CreateSession(
        Guid userId,
        string deviceId = "device-1",
        DateTime? createdAtUtc = null,
        DateTime? expiresAtUtc = null,
        bool isPersistent = true)
    {
        DateTime created = createdAtUtc ?? CreatedAtUtc;

        return UserSession.Create(
            userId: userId,
            deviceInfo: CreateDeviceInfo(deviceId),
            geoLocation: CreateGeoLocation(),
            refreshTokenExpiresAtUtc: expiresAtUtc ?? created.AddHours(8),
            isPersistent: isPersistent,
            createdAtUtc: created);
    }

    internal static RefreshToken IssueRefreshToken(
        User user,
        string tokenHash = "refresh-token-hash",
        string deviceId = "device-1",
        DateTime? createdAtUtc = null,
        DateTime? expiresAtUtc = null,
        bool isPersistent = true)
    {
        DateTime created = createdAtUtc ?? CreatedAtUtc;

        return user.IssueRefreshToken(
            sessionId: Guid.Parse("10000000-0000-0000-0000-000000000001"),
            tokenHash: tokenHash,
            expiresAtUtc: expiresAtUtc ?? created.AddDays(7),
            deviceInfo: CreateDeviceInfo(deviceId),
            geoLocation: CreateGeoLocation(),
            isPersistent: isPersistent,
            createdAtUtc: created);
    }

    internal static OneTimeToken CreateOneTimeToken(
        Guid userId,
        OneTimeTokenPurpose purpose = OneTimeTokenPurpose.PasswordReset,
        string tokenHash = "one-time-token-hash",
        DateTime? createdAtUtc = null,
        DateTime? expiresAtUtc = null)
    {
        DateTime created = createdAtUtc ?? CreatedAtUtc;

        return OneTimeToken.Create(
            userId: userId,
            purpose: purpose,
            tokenHash: tokenHash,
            expiresAtUtc: expiresAtUtc ?? created.AddMinutes(30),
            createdAtUtc: created);
    }

    internal static Role CreateRole(
        string name = "User",
        bool isSystem = false,
        DateTime? createdAtUtc = null)
    {
        return Role.Create(
            name: name,
            isSystem: isSystem,
            createdAtUtc: createdAtUtc ?? CreatedAtUtc);
    }

    internal static Permission CreatePermission(
        string key = "identity.users.read",
        string service = "Identity",
        string group = "Users",
        string description = "Read users")
    {
        return new Permission(key, service, group, description);
    }

    internal static SecurityAuditEventRecord CreateSecurityAuditRecord(
        SecurityAuditEventType eventType = SecurityAuditEventType.Login,
        bool isSuccessful = false,
        Guid? userId = null,
        string? subject = "neo@matrix.local",
        string? ipAddress = "127.0.0.1",
        DateTime? occurredAtUtc = null)
    {
        return SecurityAuditEventRecord.Create(
            entry: new SecurityAuditEntry(
                EventType: eventType,
                IsSuccessful: isSuccessful,
                UserId: userId,
                Subject: subject,
                IpAddress: ipAddress,
                UserAgent: "Mozilla/5.0",
                DeviceId: "device-1",
                DeviceName: "Pixel"),
            occurredAtUtc: occurredAtUtc ?? CreatedAtUtc);
    }

    internal static DeviceInfo CreateDeviceInfo(string deviceId = "device-1")
    {
        return DeviceInfo.Create(
            deviceId: deviceId,
            deviceName: "Pixel",
            userAgent: "Mozilla/5.0",
            ipAddress: "127.0.0.1");
    }

    internal static DomainGeoLocation CreateGeoLocation()
    {
        return DomainGeoLocation.Create(
            country: "Russia",
            region: "Zabaykalsky Krai",
            city: "Chita");
    }

    internal static IOptions<ExternalJwtOptions> CreateJwtOptions(
        int refreshTokenLifetimeDays = 7,
        int shortRefreshTokenLifetimeHours = 8)
    {
        return Options.Create(
            new ExternalJwtOptions
            {
                Issuer = "matrix",
                Audience = "matrix-clients",
                SigningKey = new string('k', 64),
                AccessTokenLifetimeMinutes = 30,
                RefreshTokenLifetimeDays = refreshTokenLifetimeDays,
                ShortRefreshTokenLifetimeHours = shortRefreshTokenLifetimeHours
            });
    }

    internal static IOptions<OneTimeTokenOptions> CreateOneTimeTokenOptions()
    {
        return Options.Create(new OneTimeTokenOptions());
    }

    internal static IOptions<SecurityAuditOptions> CreateSecurityAuditOptions(
        int failedLoginWindowMinutes = 15,
        int failedLoginMaxAttemptsPerLogin = 10,
        int failedLoginMaxAttemptsPerIp = 20,
        int emailConfirmationRequestWindowMinutes = 60,
        int emailConfirmationRequestMaxAttemptsPerEmail = 5,
        int emailConfirmationRequestMaxAttemptsPerIp = 20,
        int emailChangeRequestWindowMinutes = 60,
        int emailChangeRequestMaxAttemptsPerEmail = 5,
        int emailChangeRequestMaxAttemptsPerIp = 20,
        int passwordResetRequestWindowMinutes = 60,
        int passwordResetRequestMaxAttemptsPerEmail = 5,
        int passwordResetRequestMaxAttemptsPerIp = 20,
        int accountRecoveryRequestWindowMinutes = 60,
        int accountRecoveryRequestMaxAttemptsPerEmail = 5,
        int accountRecoveryRequestMaxAttemptsPerIp = 20)
    {
        return Options.Create(
            new SecurityAuditOptions
            {
                FailedLoginWindowMinutes = failedLoginWindowMinutes,
                FailedLoginMaxAttemptsPerLogin = failedLoginMaxAttemptsPerLogin,
                FailedLoginMaxAttemptsPerIp = failedLoginMaxAttemptsPerIp,
                EmailConfirmationRequestWindowMinutes = emailConfirmationRequestWindowMinutes,
                EmailConfirmationRequestMaxAttemptsPerEmail = emailConfirmationRequestMaxAttemptsPerEmail,
                EmailConfirmationRequestMaxAttemptsPerIp = emailConfirmationRequestMaxAttemptsPerIp,
                EmailChangeRequestWindowMinutes = emailChangeRequestWindowMinutes,
                EmailChangeRequestMaxAttemptsPerEmail = emailChangeRequestMaxAttemptsPerEmail,
                EmailChangeRequestMaxAttemptsPerIp = emailChangeRequestMaxAttemptsPerIp,
                PasswordResetRequestWindowMinutes = passwordResetRequestWindowMinutes,
                PasswordResetRequestMaxAttemptsPerEmail = passwordResetRequestMaxAttemptsPerEmail,
                PasswordResetRequestMaxAttemptsPerIp = passwordResetRequestMaxAttemptsPerIp,
                AccountRecoveryRequestWindowMinutes = accountRecoveryRequestWindowMinutes,
                AccountRecoveryRequestMaxAttemptsPerEmail = accountRecoveryRequestMaxAttemptsPerEmail,
                AccountRecoveryRequestMaxAttemptsPerIp = accountRecoveryRequestMaxAttemptsPerIp
            });
    }

    internal static IOptions<FrontendLinksOptions> CreateFrontendLinksOptions(
        string baseUrl = "https://matrix.local/app/")
    {
        return Options.Create(
            new FrontendLinksOptions
            {
                BaseUrl = baseUrl
            });
    }

    internal static IOptions<GeoLocationOptions> CreateGeoLocationOptions(
        bool enabled = true,
        string endpointTemplate = "https://ipapi.co/{ip}/json/")
    {
        return Options.Create(
            new GeoLocationOptions
            {
                Enabled = enabled,
                EndpointTemplate = endpointTemplate
            });
    }

    internal static HttpClient CreateHttpClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        return new HttpClient(new StubHttpMessageHandler(handler))
        {
            BaseAddress = new Uri("https://localhost")
        };
    }

    internal static HttpResponseMessage JsonResponse<T>(
        T payload,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json")
        };
    }

    internal static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(50);
        }

        if (!condition())
            throw new TimeoutException("Timed out waiting for expected condition.");
    }
}

internal sealed class IdentityTestDatabase(
    IdentityDbContext dbContext,
    SqliteConnection connection) : IAsyncDisposable
{
    public IdentityDbContext DbContext { get; } = dbContext;

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await connection.DisposeAsync();
    }
}

internal sealed class FrozenClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
}

internal sealed class FrozenTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

internal sealed class TestLogger<T> : ILogger<T>
{
    public List<TestLogEntry> Entries { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new TestLogEntry(logLevel, formatter(state, exception), exception));
    }
}

internal sealed record TestLogEntry(LogLevel LogLevel, string Message, Exception? Exception);

internal sealed class FakeRefreshTokenBulkRepository : IRefreshTokenBulkRepository
{
    public DateTime? LastExpiredBeforeUtc { get; private set; }
    public DateTime? LastRevokedBeforeUtc { get; private set; }
    public int? LastBatchSize { get; private set; }
    public int DeleteExpiredBatchResult { get; set; }
    public int DeleteRevokedBatchResult { get; set; }

    public Task<int> RevokeAllByUserIdAsync(Guid userId, RefreshTokenRevocationReason reason, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<int> RevokeByIdAsync(Guid userId, Guid refreshTokenId, RefreshTokenRevocationReason reason, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<int> DeleteExpiredAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<int> DeleteRevokedAndExpiredAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<int> DeleteExpiredBatchAsync(DateTime expiredBeforeUtc, int batchSize, CancellationToken cancellationToken)
    {
        LastExpiredBeforeUtc = expiredBeforeUtc;
        LastBatchSize = batchSize;
        return Task.FromResult(DeleteExpiredBatchResult);
    }

    public Task<int> DeleteRevokedBatchAsync(DateTime revokedBeforeUtc, int batchSize, CancellationToken cancellationToken)
    {
        LastRevokedBeforeUtc = revokedBeforeUtc;
        LastBatchSize = batchSize;
        return Task.FromResult(DeleteRevokedBatchResult);
    }
}

internal sealed class FakeSecurityAuditBulkRepository : ISecurityAuditBulkRepository
{
    public DateTime? LastOccurredBeforeUtc { get; private set; }
    public int? LastBatchSize { get; private set; }
    public int DeleteBatchResult { get; set; }

    public Task<int> DeleteBatchAsync(DateTime occurredBeforeUtc, int batchSize, CancellationToken cancellationToken)
    {
        LastOccurredBeforeUtc = occurredBeforeUtc;
        LastBatchSize = batchSize;
        return Task.FromResult(DeleteBatchResult);
    }
}

internal sealed class DictionaryServiceProvider(Dictionary<Type, object> services) : IServiceProvider
{
    public object? GetService(Type serviceType)
    {
        services.TryGetValue(serviceType, out object? service);
        return service;
    }
}

internal sealed class TestServiceScopeFactory(IServiceProvider serviceProvider) : IServiceScopeFactory
{
    public IServiceScope CreateScope() => new TestServiceScope(serviceProvider);
}

internal sealed class TestServiceScope(IServiceProvider serviceProvider) : IServiceScope
{
    public IServiceProvider ServiceProvider => serviceProvider;

    public void Dispose()
    {
    }
}

internal sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new();

    public void Dispose()
    {
    }
}

internal sealed class StubHttpMessageHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return handler(request, cancellationToken);
    }
}
