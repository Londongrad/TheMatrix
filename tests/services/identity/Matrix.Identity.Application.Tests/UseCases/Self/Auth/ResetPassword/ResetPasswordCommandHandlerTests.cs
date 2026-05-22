using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_WritesFailureAuditAndThrowsDomainException()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
        var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
        var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateResetPasswordHandler(
            userRepository,
            userSessionRepository,
            oneTimeTokenRepository,
            oneTimeTokenService,
            passwordHasher,
            unitOfWork,
            securityAuditService);
        Guid userId = Guid.Parse("40000000-0000-0000-0000-000000000001");

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateResetPasswordCommand(
                userId: userId,
                ipAddress: "203.0.113.41",
                userAgent: "Mozilla/5.0 (reset-missing-user)"),
            CancellationToken.None));

        Assert.Equal("Identity.OneTimeToken.NotFound", exception.Code);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.PasswordResetCompleted, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Null(audit.UserId);
        Assert.Equal(userId.ToString(), audit.Subject);
        Assert.Equal("UserNotFound", audit.Details);
        Assert.Empty(oneTimeTokenService.HashTokenInputs);
        Assert.Empty(passwordHasher.HashedPasswords);
    }

    [Fact]
    public async Task Handle_WhenUserDeleted_WritesFailureAuditAndThrowsForbidden()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByIdWithRefreshTokens = user
        };
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateResetPasswordHandler(
            userRepository,
            new SelfServiceHandlerTestSupport.FakeUserSessionRepository(),
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository(),
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
            new SelfServiceHandlerTestSupport.FakePasswordHasher(),
            unitOfWork,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateResetPasswordCommand(user.Id),
            CancellationToken.None));

        Assert.Equal("Identity.AccountDeleted", exception.Code);
        Assert.Equal(ApplicationErrorType.Forbidden, exception.ErrorType);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal("AccountDeleted", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenTokenMissing_WritesFailureAuditAndThrowsDomainException()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByIdWithRefreshTokens = user
        };
        var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
        var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateResetPasswordHandler(
            userRepository,
            new SelfServiceHandlerTestSupport.FakeUserSessionRepository(),
            oneTimeTokenRepository,
            oneTimeTokenService,
            new SelfServiceHandlerTestSupport.FakePasswordHasher(),
            unitOfWork,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateResetPasswordCommand(
                userId: user.Id,
                token: "presented-reset-token"),
            CancellationToken.None));

        Assert.Equal("Identity.OneTimeToken.NotFound", exception.Code);
        Assert.Equal(new[] { "presented-reset-token" }, oneTimeTokenService.HashTokenInputs);
        Assert.Equal(
            (user.Id, OneTimeTokenPurpose.PasswordReset, oneTimeTokenService.HashedToken),
            Assert.IsType<(Guid UserId, OneTimeTokenPurpose Purpose, string TokenHash)>(oneTimeTokenRepository.FindRequest!.Value));
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal("InvalidToken", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenTokenValid_UsesTokenChangesPasswordRevokesSessionsAndWritesAudit()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(passwordHash: "stored-hash");
        var activeSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-1");
        var otherActiveSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-2");
        var inactiveSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-3", isRevoked: true);
        var activeToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, activeSession.Id, "active-token", deviceId: "device-1");
        var otherActiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, otherActiveSession.Id, "other-active-token", deviceId: "device-2");
        var inactiveRefreshToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, inactiveSession.Id, "inactive-token", deviceId: "device-3", isRevoked: true);
        var resetToken = SelfServiceHandlerTestSupport.CreateOneTimeToken(user.Id);
        var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
        {
            FoundToken = resetToken
        };
        var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = SelfServiceHandlerTestSupport.CreateResetPasswordHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            },
            new SelfServiceHandlerTestSupport.FakeUserSessionRepository
            {
                Sessions = { activeSession, otherActiveSession, inactiveSession }
            },
            oneTimeTokenRepository,
            oneTimeTokenService,
            passwordHasher,
            unitOfWork,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateResetPasswordCommand(
                userId: user.Id,
                token: "presented-reset-token",
                newPassword: "ResetPa$$w0rd",
                ipAddress: "203.0.113.42",
                userAgent: "Mozilla/5.0 (reset-success)"),
            CancellationToken.None);

        Assert.Equal(new[] { "presented-reset-token" }, oneTimeTokenService.HashTokenInputs);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, resetToken.UsedAtUtc);
        Assert.Equal(new[] { "ResetPa$$w0rd" }, passwordHasher.HashedPasswords);
        Assert.Equal("hash::ResetPa$$w0rd", user.PasswordHash);

        Assert.True(activeSession.IsRevoked);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, activeSession.RevokedAtUtc);
        Assert.True(otherActiveSession.IsRevoked);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, otherActiveSession.RevokedAtUtc);
        Assert.True(inactiveSession.IsRevoked);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-1), inactiveSession.RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.PasswordChanged, activeSession.RevokedReason);
        Assert.Equal(RefreshTokenRevocationReason.PasswordChanged, otherActiveSession.RevokedReason);

        Assert.True(activeToken.IsRevoked);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, activeToken.RevokedAtUtc);
        Assert.True(otherActiveToken.IsRevoked);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, otherActiveToken.RevokedAtUtc);
        Assert.True(inactiveRefreshToken.IsRevoked);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-1), inactiveRefreshToken.RevokedAtUtc);
        Assert.Equal(RefreshTokenRevocationReason.PasswordChanged, activeToken.RevokedReason);
        Assert.Equal(RefreshTokenRevocationReason.PasswordChanged, otherActiveToken.RevokedReason);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);

        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.PasswordResetCompleted, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Id.ToString(), audit.Subject);
        Assert.Equal("203.0.113.42", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (reset-success)", audit.UserAgent);
        Assert.Null(audit.Details);
    }
}
