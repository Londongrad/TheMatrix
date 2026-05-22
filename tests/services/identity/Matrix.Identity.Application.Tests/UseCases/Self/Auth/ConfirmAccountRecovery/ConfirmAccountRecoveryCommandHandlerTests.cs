using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.ConfirmAccountRecovery;

public sealed class ConfirmAccountRecoveryCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_WritesFailureAuditAndThrowsDomainException()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
        var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Auth.ConfirmAccountRecovery.ConfirmAccountRecoveryCommandHandler(
            userRepository,
            oneTimeTokenRepository,
            oneTimeTokenService,
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            unitOfWork,
            securityAuditService);
        Guid userId = Guid.Parse("70000000-0000-0000-0000-000000000001");

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmAccountRecoveryCommand(
                userId: userId,
                ipAddress: "203.0.113.80",
                userAgent: "Mozilla/5.0 (recovery-missing-user)"),
            CancellationToken.None));

        Assert.Equal("Identity.OneTimeToken.NotFound", exception.Code);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.AccountRestored, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Null(audit.UserId);
        Assert.Equal(userId.ToString(), audit.Subject);
        Assert.Equal("UserNotFound", audit.Details);
        Assert.Empty(oneTimeTokenService.HashTokenInputs);
    }

    [Fact]
    public async Task Handle_WhenTokenMissing_WritesFailureAuditAndThrowsDomainException()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
        var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
        var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Auth.ConfirmAccountRecovery.ConfirmAccountRecoveryCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            oneTimeTokenRepository,
            oneTimeTokenService,
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            unitOfWork,
            securityAuditService);

        var exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmAccountRecoveryCommand(
                userId: user.Id,
                token: "presented-recovery-token",
                ipAddress: "203.0.113.81",
                userAgent: "Mozilla/5.0 (recovery-missing-token)"),
            CancellationToken.None));

        Assert.Equal("Identity.OneTimeToken.NotFound", exception.Code);
        Assert.Equal(new[] { "presented-recovery-token" }, oneTimeTokenService.HashTokenInputs);
        Assert.Equal(
            (user.Id, OneTimeTokenPurpose.AccountRecovery, oneTimeTokenService.HashedToken),
            Assert.IsType<(Guid UserId, OneTimeTokenPurpose Purpose, string TokenHash)>(oneTimeTokenRepository.FindRequest!.Value));
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.AccountRestored, audit.EventType);
        Assert.False(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("InvalidToken", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenDeletedUserUnlocked_RestoresUserAndWritesRestoredAudit()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
        int originalPermissionsVersion = user.PermissionsVersion;
        var token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
            userId: user.Id,
            purpose: OneTimeTokenPurpose.AccountRecovery);
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Auth.ConfirmAccountRecovery.ConfirmAccountRecoveryCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
            {
                FoundToken = token
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            unitOfWork,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmAccountRecoveryCommand(
                userId: user.Id,
                token: "presented-recovery-token",
                ipAddress: "203.0.113.82",
                userAgent: "Mozilla/5.0 (recovery-restored)"),
            CancellationToken.None);

        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, token.UsedAtUtc);
        Assert.False(user.IsDeleted);
        Assert.Null(user.DeletedAtUtc);
        Assert.Equal(originalPermissionsVersion + 1, user.PermissionsVersion);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.Equal(SecurityAuditEventType.AccountRestored, audit.EventType);
        Assert.True(audit.IsSuccessful);
        Assert.Equal(user.Id, audit.UserId);
        Assert.Equal(user.Email.Value, audit.Subject);
        Assert.Equal("Restored", audit.Details);
        Assert.Equal("203.0.113.82", audit.IpAddress);
        Assert.Equal("Mozilla/5.0 (recovery-restored)", audit.UserAgent);
    }

    [Fact]
    public async Task Handle_WhenDeletedLockedUser_RestoresUserAndKeepsLockedState()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true, isLocked: true);
        int originalPermissionsVersion = user.PermissionsVersion;
        var token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
            userId: user.Id,
            purpose: OneTimeTokenPurpose.AccountRecovery);
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Auth.ConfirmAccountRecovery.ConfirmAccountRecoveryCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
            {
                FoundToken = token
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            unitOfWork,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmAccountRecoveryCommand(user.Id),
            CancellationToken.None);

        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, token.UsedAtUtc);
        Assert.False(user.IsDeleted);
        Assert.True(user.IsLocked);
        Assert.Equal(originalPermissionsVersion + 1, user.PermissionsVersion);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.True(audit.IsSuccessful);
        Assert.Equal("RestoredButLocked", audit.Details);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyActive_MarksTokenUsedAndWritesAlreadyActiveAudit()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        int originalPermissionsVersion = user.PermissionsVersion;
        var token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
            userId: user.Id,
            purpose: OneTimeTokenPurpose.AccountRecovery);
        var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
        var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Auth.ConfirmAccountRecovery.ConfirmAccountRecoveryCommandHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserById = user
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
            {
                FoundToken = token
            },
            new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            unitOfWork,
            securityAuditService);

        await handler.Handle(
            SelfServiceHandlerTestSupport.CreateConfirmAccountRecoveryCommand(user.Id),
            CancellationToken.None);

        Assert.Equal(SelfServiceHandlerTestSupport.UtcNow, token.UsedAtUtc);
        Assert.False(user.IsDeleted);
        Assert.Equal(originalPermissionsVersion, user.PermissionsVersion);
        Assert.Equal(1, unitOfWork.SaveChangesCalls);
        SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
        Assert.True(audit.IsSuccessful);
        Assert.Equal("AlreadyActive", audit.Details);
    }
}
