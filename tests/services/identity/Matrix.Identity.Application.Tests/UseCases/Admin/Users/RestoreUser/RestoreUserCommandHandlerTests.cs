using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.RestoreUser;
using Matrix.Identity.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.RestoreUser
{
    public sealed class RestoreUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ThrowsNotFound()
        {
            var currentUser = new AdminUsersTestSupport.FakeCurrentUserContext
            {
                UserId = Guid.NewGuid()
            };
            var handler = new RestoreUserCommandHandler(
                userRepository: new AdminUsersTestSupport.FakeUserRepository(),
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                securityAuditService: new AdminUsersTestSupport.FakeSecurityAuditService(),
                emailSender: new AdminUsersTestSupport.FakeEmailSender(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork(),
                currentUser: currentUser,
                logger: NullLogger<RestoreUserCommandHandler>.Instance);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new RestoreUserCommand(Guid.NewGuid()),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenUserIsDeleted_RestoresUserWritesAuditAndSendsEmail()
        {
            User user = AdminUsersTestSupport.CreateUser(isDeleted: true);
            var adminUserId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                UserById = user
            };
            var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard();
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var securityAuditService = new AdminUsersTestSupport.FakeSecurityAuditService();
            var emailSender = new AdminUsersTestSupport.FakeEmailSender();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var currentUser = new AdminUsersTestSupport.FakeCurrentUserContext
            {
                UserId = adminUserId
            };
            var handler = new RestoreUserCommandHandler(
                userRepository: userRepository,
                adminUserGuard: adminUserGuard,
                securityStateChangeCollector: securityStateChangeCollector,
                securityAuditService: securityAuditService,
                emailSender: emailSender,
                unitOfWork: unitOfWork,
                currentUser: currentUser,
                logger: NullLogger<RestoreUserCommandHandler>.Instance);

            await handler.Handle(
                request: new RestoreUserCommand(user.Id),
                cancellationToken: CancellationToken.None);

            Assert.False(user.IsDeleted);
            Assert.Equal(
                expected: 2,
                actual: user.PermissionsVersion);
            Assert.Equal(
                expected: user.Id,
                actual: adminUserGuard.RequestedTargetUserId);
            Assert.Equal(
                expected: [user.Id],
                actual: securityStateChangeCollector.ChangedUsers);
            SecurityAuditEntry auditEntry = Assert.Single(securityAuditService.Entries);
            Assert.Equal(
                expected: SecurityAuditEventType.AccountRestored,
                actual: auditEntry.EventType);
            Assert.Equal(
                expected: user.Id,
                actual: auditEntry.UserId);
            Assert.Equal(
                expected: $"RestoredBy:{adminUserId:D}",
                actual: auditEntry.Details);
            Assert.Equal(
                expected: ["neo@matrix.local"],
                actual: emailSender.AccountRestoredEmails);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }

        [Fact]
        public async Task Handle_WhenUserIsNotDeleted_DoesNothingAfterTransaction()
        {
            User user = AdminUsersTestSupport.CreateUser(isDeleted: false);
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var securityAuditService = new AdminUsersTestSupport.FakeSecurityAuditService();
            var emailSender = new AdminUsersTestSupport.FakeEmailSender();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new RestoreUserCommandHandler(
                userRepository: new AdminUsersTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: securityStateChangeCollector,
                securityAuditService: securityAuditService,
                emailSender: emailSender,
                unitOfWork: unitOfWork,
                currentUser: new AdminUsersTestSupport.FakeCurrentUserContext
                {
                    UserId = Guid.NewGuid()
                },
                logger: NullLogger<RestoreUserCommandHandler>.Instance);

            await handler.Handle(
                request: new RestoreUserCommand(user.Id),
                cancellationToken: CancellationToken.None);

            Assert.Empty(securityStateChangeCollector.ChangedUsers);
            Assert.Empty(securityAuditService.Entries);
            Assert.Empty(emailSender.AccountRestoredEmails);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }
    }
}
