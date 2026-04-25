using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Roles.UpdateRolePermissions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.UpdateRolePermissions;

public sealed class UpdateRolePermissionsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleDoesNotExist_ThrowsNotFound()
    {
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        var handler = new UpdateRolePermissionsCommandHandler(
            roleReadRepository,
            new AdminRolesTestSupport.FakeRolePermissionsRepository(),
            new AdminRolesTestSupport.FakePermissionKeysValidator(),
            new AdminRolesTestSupport.FakeUserRepository(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new UpdateRolePermissionsCommand(Guid.NewGuid(), new[] { "users.read" }),
            CancellationToken.None));

        Assert.Equal("Identity.Role.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenRoleIsSystem_ThrowsForbidden()
    {
        var role = AdminRolesTestSupport.CreateRole("SuperAdmin", isSystem: true);
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        roleReadRepository.RolesById[role.Id] = role;
        var handler = new UpdateRolePermissionsCommandHandler(
            roleReadRepository,
            new AdminRolesTestSupport.FakeRolePermissionsRepository(),
            new AdminRolesTestSupport.FakePermissionKeysValidator(),
            new AdminRolesTestSupport.FakeUserRepository(),
            new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
            new AdminRolesTestSupport.FakeUnitOfWork());

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new UpdateRolePermissionsCommand(role.Id, new[] { "users.read" }),
            CancellationToken.None));

        Assert.Equal("Identity.Role.System.ReadOnly", exception.Code);
        Assert.Equal(ApplicationErrorType.Forbidden, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenPermissionsChange_ReplacesPermissionsAndMarksAffectedUsers()
    {
        var role = AdminRolesTestSupport.CreateRole("Operators");
        Guid firstUserId = Guid.NewGuid();
        Guid secondUserId = Guid.NewGuid();
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        roleReadRepository.RolesById[role.Id] = role;
        var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository
        {
            ReplaceResult = true
        };
        var permissionKeysValidator = new AdminRolesTestSupport.FakePermissionKeysValidator();
        var userRepository = new AdminRolesTestSupport.FakeUserRepository();
        userRepository.UserIdsByRoleId[role.Id] = new[] { firstUserId, secondUserId };
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new UpdateRolePermissionsCommandHandler(
            roleReadRepository,
            rolePermissionsRepository,
            permissionKeysValidator,
            userRepository,
            securityStateChangeCollector,
            unitOfWork);

        await handler.Handle(
            new UpdateRolePermissionsCommand(role.Id, new[] { " users.read ", "users.read", "", "roles.manage" }),
            CancellationToken.None);

        Assert.Equal(role.Id, rolePermissionsRepository.RequestedRoleId);
        Assert.NotNull(permissionKeysValidator.ValidatedKeys);
        Assert.NotNull(rolePermissionsRepository.RequestedPermissionKeys);
        Assert.Equal(
            new HashSet<string>(new[] { "users.read", "roles.manage" }, StringComparer.Ordinal),
            permissionKeysValidator.ValidatedKeys!.ToHashSet(StringComparer.Ordinal));
        Assert.Equal(
            new HashSet<string>(new[] { "users.read", "roles.manage" }, StringComparer.Ordinal),
            rolePermissionsRepository.RequestedPermissionKeys!.ToHashSet(StringComparer.Ordinal));
        Assert.Equal(role.Id, userRepository.RequestedRoleId);
        Assert.Equal(new[] { firstUserId, secondUserId }, securityStateChangeCollector.ChangedUsers);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task Handle_WhenPermissionsDoNotChange_DoesNotMarkUsers()
    {
        var role = AdminRolesTestSupport.CreateRole("Operators");
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        roleReadRepository.RolesById[role.Id] = role;
        var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository
        {
            ReplaceResult = false
        };
        var permissionKeysValidator = new AdminRolesTestSupport.FakePermissionKeysValidator();
        var userRepository = new AdminRolesTestSupport.FakeUserRepository();
        var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
        var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
        var handler = new UpdateRolePermissionsCommandHandler(
            roleReadRepository,
            rolePermissionsRepository,
            permissionKeysValidator,
            userRepository,
            securityStateChangeCollector,
            unitOfWork);

        await handler.Handle(
            new UpdateRolePermissionsCommand(role.Id, new[] { "users.read" }),
            CancellationToken.None);

        Assert.NotNull(permissionKeysValidator.ValidatedKeys);
        Assert.Equal(role.Id, rolePermissionsRepository.RequestedRoleId);
        Assert.Null(userRepository.RequestedRoleId);
        Assert.Empty(securityStateChangeCollector.ChangedUsers);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }
}
