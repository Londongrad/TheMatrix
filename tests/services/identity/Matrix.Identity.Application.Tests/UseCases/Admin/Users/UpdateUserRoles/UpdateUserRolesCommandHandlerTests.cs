using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserRoles;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UpdateUserRoles
{
    public sealed class UpdateUserRolesCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenRolesDoNotChange_DoesNotMarkSecurityState()
        {
            var userId = Guid.NewGuid();
            Guid[] roleIds =
            [
                Guid.NewGuid(),
                Guid.NewGuid()
            ];
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = true
            };
            var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository
            {
                ReplaceResult = false
            };
            var roleIdsValidator = new AdminUsersTestSupport.FakeRoleIdsValidator();
            var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard();
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new UpdateUserRolesCommandHandler(
                userRepository: userRepository,
                userRolesRepository: userRolesRepository,
                roleIdsValidator: roleIdsValidator,
                adminUserGuard: adminUserGuard,
                unitOfWork: unitOfWork,
                securityStateChangeCollector: securityStateChangeCollector);

            await handler.Handle(
                request: new UpdateUserRolesCommand(
                    UserId: userId,
                    RoleIds: roleIds),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: roleIds.ToHashSet(),
                actual: roleIdsValidator.ValidatedRoleIds!.ToHashSet());
            Assert.Equal(
                expected: roleIds.ToHashSet(),
                actual: adminUserGuard.RequestedDesiredRoleIds!.ToHashSet());
            Assert.Equal(
                expected: roleIds.ToHashSet(),
                actual: userRolesRepository.ReplacedRoleIds!.ToHashSet());
            Assert.Empty(securityStateChangeCollector.ChangedUsers);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }

        [Fact]
        public async Task Handle_WhenRolesChange_MarksUserChanged()
        {
            var userId = Guid.NewGuid();
            Guid[] roleIds = [Guid.NewGuid()];
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = true
            };
            var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository
            {
                ReplaceResult = true
            };
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new UpdateUserRolesCommandHandler(
                userRepository: userRepository,
                userRolesRepository: userRolesRepository,
                roleIdsValidator: new AdminUsersTestSupport.FakeRoleIdsValidator(),
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                unitOfWork: unitOfWork,
                securityStateChangeCollector: securityStateChangeCollector);

            await handler.Handle(
                request: new UpdateUserRolesCommand(
                    UserId: userId,
                    RoleIds: roleIds),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: [userId],
                actual: securityStateChangeCollector.ChangedUsers);
            Assert.Equal(
                expected: userId,
                actual: userRolesRepository.RequestedUserId);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }
    }
}
