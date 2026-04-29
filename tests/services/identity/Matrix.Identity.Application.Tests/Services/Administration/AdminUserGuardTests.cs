using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Services;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.Tests.UseCases.Admin.Users;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.Services.Administration;

public sealed class AdminUserGuardTests
{
    [Fact]
    public async Task EnsureUserCanBeManagedAsync_WhenTargetIsCurrentUser_ThrowsForbidden()
    {
        Guid currentUserId = Guid.NewGuid();
        var currentUser = new AdminUsersTestSupport.FakeCurrentUserContext
        {
            UserId = currentUserId
        };
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository();
        var guard = new AdminUserGuard(currentUser, roleReadRepository, userRolesRepository);

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(
            () => guard.EnsureUserCanBeManagedAsync(currentUserId, CancellationToken.None));

        Assert.Equal("Identity.Admin.SelfActionForbidden", exception.Code);
    }

    [Fact]
    public async Task EnsureUserCanBeManagedAsync_WhenTargetIsSuperAdmin_ThrowsForbidden()
    {
        Guid currentUserId = Guid.NewGuid();
        Guid targetUserId = Guid.NewGuid();
        var currentUser = new AdminUsersTestSupport.FakeCurrentUserContext
        {
            UserId = currentUserId
        };
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository
        {
            GetUserRolesResult =
            [
                new UserRoleResult
                {
                    Id = Guid.NewGuid(),
                    Name = SystemRoleNames.SuperAdmin,
                    IsSystem = true,
                    CreatedAtUtc = AdminUsersTestSupport.UtcNow
                }
            ]
        };
        var guard = new AdminUserGuard(currentUser, roleReadRepository, userRolesRepository);

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(
            () => guard.EnsureUserCanBeManagedAsync(targetUserId, CancellationToken.None));

        Assert.Equal("Identity.Admin.SuperAdmin.Protected", exception.Code);
        Assert.Equal(targetUserId, userRolesRepository.RequestedUserId);
    }

    [Fact]
    public async Task EnsureRoleAssignmentIsAllowedAsync_WhenSuperAdminRoleIsRequested_ThrowsForbidden()
    {
        var currentUser = new AdminUsersTestSupport.FakeCurrentUserContext
        {
            UserId = Guid.NewGuid()
        };
        Role superAdminRole = AdminRolesTestSupport.CreateRole(SystemRoleNames.SuperAdmin, isSystem: true);
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        roleReadRepository.RolesById[superAdminRole.Id] = superAdminRole;
        var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository();
        var guard = new AdminUserGuard(currentUser, roleReadRepository, userRolesRepository);

        MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(
            () => guard.EnsureRoleAssignmentIsAllowedAsync([superAdminRole.Id], CancellationToken.None));

        Assert.Equal("Identity.Admin.SuperAdmin.RoleAssignmentForbidden", exception.Code);
    }

    [Fact]
    public async Task EnsureRoleAssignmentIsAllowedAsync_WhenSuperAdminRoleIsAbsent_Completes()
    {
        var currentUser = new AdminUsersTestSupport.FakeCurrentUserContext
        {
            UserId = Guid.NewGuid()
        };
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository();
        var guard = new AdminUserGuard(currentUser, roleReadRepository, userRolesRepository);

        await guard.EnsureRoleAssignmentIsAllowedAsync([Guid.NewGuid()], CancellationToken.None);
    }
}
