using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Services;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.Tests.UseCases.Admin.Users;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Matrix.Identity.Domain.Authorization;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.Services.Administration
{
    public sealed class AdminUserGuardTests
    {
        [Fact]
        public async Task EnsureUserCanBeManagedAsync_WhenTargetIsCurrentUser_ThrowsForbidden()
        {
            var currentUserId = Guid.NewGuid();
            var currentUser = new AdminUsersTestSupport.FakeCurrentUserContext
            {
                UserId = currentUserId
            };
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository();
            var guard = new AdminUserGuard(
                currentUserContext: currentUser,
                roleReadRepository: roleReadRepository,
                userRolesRepository: userRolesRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => guard.EnsureUserCanBeManagedAsync(
                    targetUserId: currentUserId,
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Admin.SelfActionForbidden",
                actual: exception.Code);
        }

        [Fact]
        public async Task EnsureUserCanBeManagedAsync_WhenTargetIsSuperAdmin_ThrowsForbidden()
        {
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
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
            var guard = new AdminUserGuard(
                currentUserContext: currentUser,
                roleReadRepository: roleReadRepository,
                userRolesRepository: userRolesRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => guard.EnsureUserCanBeManagedAsync(
                    targetUserId: targetUserId,
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Admin.SuperAdmin.Protected",
                actual: exception.Code);
            Assert.Equal(
                expected: targetUserId,
                actual: userRolesRepository.RequestedUserId);
        }

        [Fact]
        public async Task EnsureRoleAssignmentIsAllowedAsync_WhenSuperAdminRoleIsRequested_ThrowsForbidden()
        {
            var currentUser = new AdminUsersTestSupport.FakeCurrentUserContext
            {
                UserId = Guid.NewGuid()
            };
            Role superAdminRole = AdminRolesTestSupport.CreateRole(
                name: SystemRoleNames.SuperAdmin,
                isSystem: true);
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            roleReadRepository.RolesById[superAdminRole.Id] = superAdminRole;
            var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository();
            var guard = new AdminUserGuard(
                currentUserContext: currentUser,
                roleReadRepository: roleReadRepository,
                userRolesRepository: userRolesRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => guard.EnsureRoleAssignmentIsAllowedAsync(
                    desiredRoleIds: [superAdminRole.Id],
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Admin.SuperAdmin.RoleAssignmentForbidden",
                actual: exception.Code);
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
            var guard = new AdminUserGuard(
                currentUserContext: currentUser,
                roleReadRepository: roleReadRepository,
                userRolesRepository: userRolesRepository);

            await guard.EnsureRoleAssignmentIsAllowedAsync(
                desiredRoleIds: [Guid.NewGuid()],
                cancellationToken: CancellationToken.None);
        }
    }
}
