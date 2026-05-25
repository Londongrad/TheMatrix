using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.UseCases.Self.Auth.ConfirmAccountRecovery;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.ConfirmAccountRecovery
{
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
            var handler = new ConfirmAccountRecoveryCommandHandler(
                userRepository: userRepository,
                oneTimeTokenRepository: oneTimeTokenRepository,
                oneTimeTokenService: oneTimeTokenService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);
            var userId = Guid.Parse("70000000-0000-0000-0000-000000000001");

            DomainException exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateConfirmAccountRecoveryCommand(
                    userId: userId,
                    ipAddress: "203.0.113.80",
                    userAgent: "Mozilla/5.0 (recovery-missing-user)"),
                cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.OneTimeToken.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.AccountRestored,
                actual: audit.EventType);
            Assert.False(audit.IsSuccessful);
            Assert.Null(audit.UserId);
            Assert.Equal(
                expected: userId.ToString(),
                actual: audit.Subject);
            Assert.Equal(
                expected: "UserNotFound",
                actual: audit.Details);
            Assert.Empty(oneTimeTokenService.HashTokenInputs);
        }

        [Fact]
        public async Task Handle_WhenTokenMissing_WritesFailureAuditAndThrowsDomainException()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
            var oneTimeTokenRepository = new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository();
            var oneTimeTokenService = new SelfServiceHandlerTestSupport.FakeOneTimeTokenService();
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmAccountRecoveryCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                oneTimeTokenRepository: oneTimeTokenRepository,
                oneTimeTokenService: oneTimeTokenService,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            DomainException exception = await Assert.ThrowsAsync<DomainException>(() => handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateConfirmAccountRecoveryCommand(
                    userId: user.Id,
                    token: "presented-recovery-token",
                    ipAddress: "203.0.113.81",
                    userAgent: "Mozilla/5.0 (recovery-missing-token)"),
                cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.OneTimeToken.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: new[]
                {
                    "presented-recovery-token"
                },
                actual: oneTimeTokenService.HashTokenInputs);
            Assert.Equal(
                expected: (user.Id, OneTimeTokenPurpose.AccountRecovery, oneTimeTokenService.HashedToken),
                actual: Assert.IsType<(Guid UserId, OneTimeTokenPurpose Purpose, string TokenHash)>(
                    oneTimeTokenRepository.FindRequest!.Value));
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.AccountRestored,
                actual: audit.EventType);
            Assert.False(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: user.Email.Value,
                actual: audit.Subject);
            Assert.Equal(
                expected: "InvalidToken",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenDeletedUserUnlocked_RestoresUserAndWritesRestoredAudit()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
            int originalPermissionsVersion = user.PermissionsVersion;
            OneTimeToken token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.AccountRecovery);
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmAccountRecoveryCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                oneTimeTokenRepository: new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
                {
                    FoundToken = token
                },
                oneTimeTokenService: new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateConfirmAccountRecoveryCommand(
                    userId: user.Id,
                    token: "presented-recovery-token",
                    ipAddress: "203.0.113.82",
                    userAgent: "Mozilla/5.0 (recovery-restored)"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: token.UsedAtUtc);
            Assert.False(user.IsDeleted);
            Assert.Null(user.DeletedAtUtc);
            Assert.Equal(
                expected: originalPermissionsVersion + 1,
                actual: user.PermissionsVersion);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.AccountRestored,
                actual: audit.EventType);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: user.Id,
                actual: audit.UserId);
            Assert.Equal(
                expected: user.Email.Value,
                actual: audit.Subject);
            Assert.Equal(
                expected: "Restored",
                actual: audit.Details);
            Assert.Equal(
                expected: "203.0.113.82",
                actual: audit.IpAddress);
            Assert.Equal(
                expected: "Mozilla/5.0 (recovery-restored)",
                actual: audit.UserAgent);
        }

        [Fact]
        public async Task Handle_WhenDeletedLockedUser_RestoresUserAndKeepsLockedState()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(
                isDeleted: true,
                isLocked: true);
            int originalPermissionsVersion = user.PermissionsVersion;
            OneTimeToken token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.AccountRecovery);
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmAccountRecoveryCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                oneTimeTokenRepository: new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
                {
                    FoundToken = token
                },
                oneTimeTokenService: new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateConfirmAccountRecoveryCommand(user.Id),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: token.UsedAtUtc);
            Assert.False(user.IsDeleted);
            Assert.True(user.IsLocked);
            Assert.Equal(
                expected: originalPermissionsVersion + 1,
                actual: user.PermissionsVersion);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: "RestoredButLocked",
                actual: audit.Details);
        }

        [Fact]
        public async Task Handle_WhenUserAlreadyActive_MarksTokenUsedAndWritesAlreadyActiveAudit()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            int originalPermissionsVersion = user.PermissionsVersion;
            OneTimeToken token = SelfServiceHandlerTestSupport.CreateOneTimeToken(
                userId: user.Id,
                purpose: OneTimeTokenPurpose.AccountRecovery);
            var unitOfWork = new SelfServiceHandlerTestSupport.FakeUnitOfWork();
            var securityAuditService = new SelfServiceHandlerTestSupport.FakeSecurityAuditService();
            var handler = new ConfirmAccountRecoveryCommandHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                oneTimeTokenRepository: new SelfServiceHandlerTestSupport.FakeOneTimeTokenRepository
                {
                    FoundToken = token
                },
                oneTimeTokenService: new SelfServiceHandlerTestSupport.FakeOneTimeTokenService(),
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                unitOfWork: unitOfWork,
                securityAuditService: securityAuditService);

            await handler.Handle(
                request: SelfServiceHandlerTestSupport.CreateConfirmAccountRecoveryCommand(user.Id),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: SelfServiceHandlerTestSupport.UtcNow,
                actual: token.UsedAtUtc);
            Assert.False(user.IsDeleted);
            Assert.Equal(
                expected: originalPermissionsVersion,
                actual: user.PermissionsVersion);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.SaveChangesCalls);
            SecurityAuditEntry audit = Assert.Single(securityAuditService.Entries);
            Assert.True(audit.IsSuccessful);
            Assert.Equal(
                expected: "AlreadyActive",
                actual: audit.Details);
        }
    }
}
