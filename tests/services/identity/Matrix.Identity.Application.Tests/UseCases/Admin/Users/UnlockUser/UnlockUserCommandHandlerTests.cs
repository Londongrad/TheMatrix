using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.UnlockUser;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UnlockUser
{
    public sealed class UnlockUserCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ThrowsNotFound()
        {
            var handler = new UnlockUserCommandHandler(
                userRepository: new AdminUsersTestSupport.FakeUserRepository(),
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new UnlockUserCommand(Guid.NewGuid()),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenUserWasLocked_UnlocksAndMarksUserChanged()
        {
            User user = AdminUsersTestSupport.CreateUser(isLocked: true);
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                UserById = user
            };
            var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard();
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new UnlockUserCommandHandler(
                userRepository: userRepository,
                adminUserGuard: adminUserGuard,
                securityStateChangeCollector: securityStateChangeCollector,
                unitOfWork: unitOfWork);

            await handler.Handle(
                request: new UnlockUserCommand(user.Id),
                cancellationToken: CancellationToken.None);

            Assert.False(user.IsLocked);
            Assert.Equal(
                expected: user.Id,
                actual: adminUserGuard.RequestedTargetUserId);
            Assert.Equal(
                expected: [user.Id],
                actual: securityStateChangeCollector.ChangedUsers);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }

        [Fact]
        public async Task Handle_WhenUserAlreadyUnlocked_DoesNotMarkSecurityStateAgain()
        {
            User user = AdminUsersTestSupport.CreateUser(isLocked: false);
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var handler = new UnlockUserCommandHandler(
                userRepository: new AdminUsersTestSupport.FakeUserRepository
                {
                    UserById = user
                },
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                securityStateChangeCollector: securityStateChangeCollector,
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            await handler.Handle(
                request: new UnlockUserCommand(user.Id),
                cancellationToken: CancellationToken.None);

            Assert.Empty(securityStateChangeCollector.ChangedUsers);
        }
    }
}
