using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.UseCases.Admin.Users.UpdateUserRoles;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.UpdateUserRoles;

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
            userRepository,
            new AdminUsersTestSupport.FakeUserRolesRepository(),
            new AdminUsersTestSupport.FakeRoleIdsValidator(),
            new AdminUsersTestSupport.FakeAdminUserGuard(),
            new AdminRolesTestSupport.FakeUnitOfWork(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new UpdateUserRolesCommand(Guid.NewGuid(), Array.Empty<Guid>()),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenAdminGuardRejectsUser_StopsBeforeRoleValidation()
    {
        Guid userId = Guid.NewGuid();
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
            userRepository,
            new AdminUsersTestSupport.FakeUserRolesRepository(),
            roleIdsValidator,
            adminUserGuard,
            new AdminRolesTestSupport.FakeUnitOfWork(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new UpdateUserRolesCommand(userId, [Guid.NewGuid()]),
            CancellationToken.None));

        Assert.Equal("Identity.Admin.SelfActionForbidden", exception.Code);
        Assert.Equal(userId, adminUserGuard.RequestedTargetUserId);
        Assert.Null(roleIdsValidator.ValidatedRoleIds);
    }

    [Fact]
    public async Task Handle_WhenRoleValidationFails_PropagatesValidationError()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var expectedRoleIds = new[] { Guid.NewGuid() };
        var roleIdsValidator = new AdminUsersTestSupport.FakeRoleIdsValidator
        {
            ValidateException = new MatrixApplicationException(
                code: "Identity.Role.NotFound",
                message: "Role not found.",
                errorType: ApplicationErrorType.NotFound)
        };
        var adminUserGuard = new AdminUsersTestSupport.FakeAdminUserGuard();
        var handler = new UpdateUserRolesCommandHandler(
            userRepository,
            new AdminUsersTestSupport.FakeUserRolesRepository(),
            roleIdsValidator,
            adminUserGuard,
            new AdminRolesTestSupport.FakeUnitOfWork(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new UpdateUserRolesCommand(userId, expectedRoleIds),
            CancellationToken.None));

        Assert.Equal("Identity.Role.NotFound", exception.Code);
        Assert.Equal(userId, adminUserGuard.RequestedTargetUserId);
        Assert.Equal(expectedRoleIds, roleIdsValidator.ValidatedRoleIds);
    }
}
