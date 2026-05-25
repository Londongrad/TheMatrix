using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserRoles;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UpdateUserRoles
{
    public sealed class UpdateUserRolesCommandHandlerFailureTests
    {
        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ThrowsNotFound()
        {
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = false
            };
            var handler = new UpdateUserRolesCommandHandler(
                userRepository: userRepository,
                userRolesRepository: new AdminUsersTestSupport.FakeUserRolesRepository(),
                roleIdsValidator: new AdminUsersTestSupport.FakeRoleIdsValidator(),
                adminUserGuard: new AdminUsersTestSupport.FakeAdminUserGuard(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new UpdateUserRolesCommand(
                        UserId: Guid.NewGuid(),
                        RoleIds: Array.Empty<Guid>()),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenAdminGuardRejectsUser_StopsBeforeRoleValidation()
        {
            var userId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = true
            };
            var roleIdsValidator = new AdminUsersTestSupport.FakeRoleIdsValidator();
            var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard
            {
                ManageException = new MatrixApplicationException(
                    code: "Identity.Admin.SelfActionForbidden",
                    message: "Self action is forbidden.",
                    errorType: ApplicationErrorType.Forbidden)
            };
            var handler = new UpdateUserRolesCommandHandler(
                userRepository: userRepository,
                userRolesRepository: new AdminUsersTestSupport.FakeUserRolesRepository(),
                roleIdsValidator: roleIdsValidator,
                adminUserGuard: adminUserGuard,
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new UpdateUserRolesCommand(
                        UserId: userId,
                        RoleIds: [Guid.NewGuid()]),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Admin.SelfActionForbidden",
                actual: exception.Code);
            Assert.Equal(
                expected: userId,
                actual: adminUserGuard.RequestedTargetUserId);
            Assert.Null(roleIdsValidator.ValidatedRoleIds);
        }

        [Fact]
        public async Task Handle_WhenRoleValidationFails_PropagatesValidationError()
        {
            var userId = Guid.NewGuid();
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = true
            };
            Guid[] expectedRoleIds = new[]
            {
                Guid.NewGuid()
            };
            var roleIdsValidator = new AdminUsersTestSupport.FakeRoleIdsValidator
            {
                ValidateException = new MatrixApplicationException(
                    code: "Identity.Role.NotFound",
                    message: "Role not found.",
                    errorType: ApplicationErrorType.NotFound)
            };
            var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard();
            var handler = new UpdateUserRolesCommandHandler(
                userRepository: userRepository,
                userRolesRepository: new AdminUsersTestSupport.FakeUserRolesRepository(),
                roleIdsValidator: roleIdsValidator,
                adminUserGuard: adminUserGuard,
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new UpdateUserRolesCommand(
                        UserId: userId,
                        RoleIds: expectedRoleIds),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: userId,
                actual: adminUserGuard.RequestedTargetUserId);
            Assert.Equal(
                expected: expectedRoleIds,
                actual: roleIdsValidator.ValidatedRoleIds);
        }
    }
}
