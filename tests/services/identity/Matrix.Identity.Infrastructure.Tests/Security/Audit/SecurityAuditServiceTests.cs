using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Infrastructure.Persistence.Models;
using Matrix.Identity.Infrastructure.Security.Audit;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Matrix.Identity.Infrastructure.Tests.TestSupport;
using static Matrix.Identity.Infrastructure.Tests.TestSupport.IdentityInfrastructureTestSupport;

namespace Matrix.Identity.Infrastructure.Tests.Security.Audit;

public sealed class SecurityAuditServiceTests
{
    [Fact]
    public async Task WriteAsync_WritesNormalizedAuditRecordUsingClockTimestamp()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        var service = new SecurityAuditService(
            dbContext: database.DbContext,
            clock: CreateClock(LaterUtc),
            options: CreateSecurityAuditOptions(),
            logger: new TestLogger<SecurityAuditService>());

        await service.WriteAsync(
            new SecurityAuditEntry(
                EventType: SecurityAuditEventType.Login,
                IsSuccessful: true,
                Subject: " neo@matrix.local ",
                IpAddress: " 127.0.0.1 ",
                DeviceName: " Pixel "),
            CancellationToken.None);
        await database.DbContext.SaveChangesAsync();

        SecurityAuditEventRecord record = await database.DbContext.SecurityAuditEvents.SingleAsync();

        Assert.Equal(LaterUtc, record.OccurredAtUtc);
        Assert.Equal("neo@matrix.local", record.Subject);
        Assert.Equal("127.0.0.1", record.IpAddress);
        Assert.Equal("Pixel", record.DeviceName);
    }

    [Fact]
    public async Task IsLoginAllowedAsync_WhenFailedAttemptsByLoginReachLimit_ReturnsFalse()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        database.DbContext.SecurityAuditEvents.AddRange(
            CreateSecurityAuditRecord(subject: "neo@matrix.local", occurredAtUtc: LaterUtc.AddMinutes(-20)),
            CreateSecurityAuditRecord(subject: "neo@matrix.local", occurredAtUtc: LaterUtc));
        await database.DbContext.SaveChangesAsync();

        var service = new SecurityAuditService(
            dbContext: database.DbContext,
            clock: CreateClock(LaterUtc.AddMinutes(5)),
            options: CreateSecurityAuditOptions(
                failedLoginWindowMinutes: 60,
                failedLoginMaxAttemptsPerLogin: 2),
            logger: new TestLogger<SecurityAuditService>());

        bool isAllowed = await service.IsLoginAllowedAsync(
            "neo@matrix.local",
            "127.0.0.1",
            CancellationToken.None);

        Assert.False(isAllowed);
    }

    [Fact]
    public async Task IsEmailChangeRequestAllowedAsync_WhenCombinedEventsReachIpLimit_ReturnsFalse()
    {
        await using IdentityTestDatabase database = CreateDbContext();
        database.DbContext.SecurityAuditEvents.AddRange(
            CreateSecurityAuditRecord(
                eventType: SecurityAuditEventType.EmailChangeRequested,
                isSuccessful: true,
                subject: "neo@matrix.local",
                ipAddress: "10.0.0.1",
                occurredAtUtc: LaterUtc.AddMinutes(-15)),
            CreateSecurityAuditRecord(
                eventType: SecurityAuditEventType.EmailChangeConfirmationResent,
                isSuccessful: true,
                subject: "neo@matrix.local",
                ipAddress: "10.0.0.1",
                occurredAtUtc: LaterUtc));
        await database.DbContext.SaveChangesAsync();

        var service = new SecurityAuditService(
            dbContext: database.DbContext,
            clock: CreateClock(LaterUtc.AddMinutes(1)),
            options: CreateSecurityAuditOptions(
                emailChangeRequestWindowMinutes: 60,
                emailChangeRequestMaxAttemptsPerIp: 2),
            logger: new TestLogger<SecurityAuditService>());

        bool isAllowed = await service.IsEmailChangeRequestAllowedAsync(
            normalizedEmail: "neo@matrix.local",
            ipAddress: "10.0.0.1",
            cancellationToken: CancellationToken.None);

        Assert.False(isAllowed);
    }
}
