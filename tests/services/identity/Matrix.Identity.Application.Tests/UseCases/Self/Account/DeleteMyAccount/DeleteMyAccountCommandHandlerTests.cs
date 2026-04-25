using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Abstractions.Services;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.DeleteMyAccount;

public sealed class DeleteMyAccountCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher();
        var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = Guid.Parse("30000000-0000-0000-0000-000000000002")
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommandHandler(
            userRepository,
            userSessionRepository,
            passwordHasher,
            emailSender,
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            currentUser,
            NullLogger<Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateDeleteMyAccountCommand(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(0, unitOfWork.SaveChangesCalls);
        Assert.Empty(securityAuditService.Entries);
        Assert.Empty(emailSender.AccountDeletedEmails);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordMissing_ThrowsValidation()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            UserByIdWithRefreshTokens = user
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommandHandler(
            userRepository,
            new SelfServiceHandlerTestSupport.FakeUserSessionRepository(),
            new SelfServiceHandlerTestSupport.FakePasswordHasher(),
            new SelfServiceHandlerTestSupport.FakeEmailSender(),
            new SelfServiceHandlerTestSupport.FakeSecurityAuditService(),
            new SelfServiceHandlerTestSupport.TestClock(),
            new SelfServiceHandlerTestSupport.FakeUnitOfWork(),
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id },
            NullLogger<Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateDeleteMyAccountCommand(currentPassword: " "),
            CancellationToken.None));

        Assert.Equal("Identity.AccountDeletionRequiresPassword", exception.Code);
        Assert.Equal(ApplicationErrorType.Validation, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordInvalid_WritesFailureAuditAndThrows()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var passwordHasher = new SelfServiceHandlerTestSupport.FakePasswordHasher
        {
            VerifyOutcome = PasswordVerificationOutcome.Failed
        };
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            },
            new SelfServiceHandlerTestSupport.FakeUserSessionRepository(),
            passwordHasher,
            new SelfServiceHandlerTestSupport.FakeEmailSender(),
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id },
            NullLogger<Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommandHandler>.Instance);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateDeleteMyAccountCommand(
                currentPassword: "WrongPa$$w0rd",
                ipAddress: "203.0.113.30",
                userAgent: "Mozilla/5.0 (delete)"),
            CancellationToken.None));

        Assert.Equal("Identity.InvalidCurrentPassword", exception.Code);
        Assert.Equal(ApplicationErrorType.Unauthorized, exception.ErrorType);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.AccountDeleted, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("InvalidCurrentPassword", audit.Details);
        Assert.Equal("203.0.113.30", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (delete)", audit.UserAgent);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyDeleted_WritesAuditAndReturnsWithoutSendingEmail()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
        var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            },
            new SelfServiceHandlerTestSupport.FakeUserSessionRepository(),
            new SelfServiceHandlerTestSupport.FakePasswordHasher(),
            emailSender,
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id },
            NullLogger<Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommandHandler>.Instance);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateDeleteMyAccountCommand(),
            CancellationToken.None);

        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.True(audit.IsSuccessful);
        Assert.Equal("AlreadyDeleted", audit.Details);
        Assert.Empty(emailSender.AccountDeletedEmails);
    }

    [Fact]
    public async Task Handle_WhenPasswordValid_RevokesSessionsAndRefreshTokensDeletesUserAndSendsEmail()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        int originalPermissionsVersion = user.PermissionsVersion;
        var activeSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-1");
        var otherActiveSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-2");
        var inactiveSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-3", isRevoked: true);
        var activeToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, activeSession.Id, "active-token", deviceId: "device-1");
        var otherActiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, otherActiveSession.Id, "other-active-token", deviceId: "device-2");
        var inactiveToken = SelfServiceHandlerTestSupport.SeedRefreshToken(user, inactiveSession.Id, "inactive-token", deviceId: "device-3", isRevoked: true);
        var emailSender = new SelfServiceHandlerTestSupport.FakeEmailSender();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            },
            new SelfServiceHandlerTestSupport.FakeUserSessionRepository
            {
                Sessions = { activeSession, otherActiveSession, inactiveSession }
            },
            new SelfServiceHandlerTestSupport.FakePasswordHasher(),
            emailSender,
            securityAuditService,
            new SelfServiceHandlerTestSupport.TestClock(),
            unitOfWork,
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id },
            NullLogger<Matrix.Identity.Application.UseCases.Self.Account.DeleteMyAccount.DeleteMyAccountCommandHandler>.Instance);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateDeleteMyAccountCommand(
                ipAddress: "203.0.113.31",
                userAgent: "Mozilla/5.0 (delete-success)"),
            CancellationToken.None);

        Assert.True(activeSession.IsRevoked);
        Assert.True(otherActiveSession.IsRevoked);
        Assert.True(inactiveSession.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.AccountDeleted, activeSession.RevokedReason);
        Assert.Equal(RefreshTokenRevocationReason.AccountDeleted, otherActiveSession.RevokedReason);

        Assert.True(activeToken.IsRevoked);
        Assert.True(otherActiveToken.IsRevoked);
        Assert.True(inactiveToken.IsRevoked);
        Assert.Equal(RefreshTokenRevocationReason.AccountDeleted, activeToken.RevokedReason);
        Assert.Equal(RefreshTokenRevocationReason.AccountDeleted, otherActiveToken.RevokedReason);

        Assert.True(user.IsDeleted);
        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, user.DeletedAtUtc);
        Assert.Equal(originalPermissionsVersion + 1, user.PermissionsVersion);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        Assert.Equal(new[] { user.Email.Value }, emailSender.AccountDeletedEmails);

        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.AccountDeleted, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("203.0.113.31", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (delete-success)", audit.UserAgent);
        Assert.StartsWith("DeletedAtUtc:", audit.Details, StringComparison.Ordinal);
    }
}
