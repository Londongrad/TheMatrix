using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Roles.UpdateRolePermissions;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.UpdateRolePermissions
{
    public sealed class UpdateRolePermissionsCommandHandlerTests
    {
        [Fact]
        public async Task Handle_WhenRoleDoesNotExist_ThrowsNotFound()
        {
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            var handler = new UpdateRolePermissionsCommandHandler(
                roleReadRepository: roleReadRepository,
                rolePermissionsRepository: new AdminRolesTestSupport.FakeRolePermissionsRepository(),
                permissionKeysValidator: new AdminRolesTestSupport.FakePermissionKeysValidator(),
                userRepository: new AdminRolesTestSupport.FakeUserRepository(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new UpdateRolePermissionsCommand(
                        RoleId: Guid.NewGuid(),
                        RolePermissionKeys: new[]
                        {
                            "users.read"
                        }),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenRoleIsSystem_ThrowsForbidden()
        {
            Role role = AdminRolesTestSupport.CreateRole(
                name: "SuperAdmin",
                isSystem: true);
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            roleReadRepository.RolesById[role.Id] = role;
            var handler = new UpdateRolePermissionsCommandHandler(
                roleReadRepository: roleReadRepository,
                rolePermissionsRepository: new AdminRolesTestSupport.FakeRolePermissionsRepository(),
                permissionKeysValidator: new AdminRolesTestSupport.FakePermissionKeysValidator(),
                userRepository: new AdminRolesTestSupport.FakeUserRepository(),
                securityStateChangeCollector: new AdminRolesTestSupport.FakeSecurityStateChangeCollector(),
                unitOfWork: new AdminRolesTestSupport.FakeUnitOfWork());

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new UpdateRolePermissionsCommand(
                        RoleId: role.Id,
                        RolePermissionKeys: new[]
                        {
                            "users.read"
                        }),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.System.ReadOnly",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Forbidden,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenPermissionsChange_ReplacesPermissionsAndMarksAffectedUsers()
        {
            Role role = AdminRolesTestSupport.CreateRole();
            var firstUserId = Guid.NewGuid();
            var secondUserId = Guid.NewGuid();
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            roleReadRepository.RolesById[role.Id] = role;
            var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository
            {
                ReplaceResult = true
            };
            var permissionKeysValidator = new AdminRolesTestSupport.FakePermissionKeysValidator();
            var userRepository = new AdminRolesTestSupport.FakeUserRepository();
            userRepository.UserIdsByRoleId[role.Id] = new[]
            {
                firstUserId,
                secondUserId
            };
            var securityStateChangeCollector = new AdminRolesTestSupport.FakeSecurityStateChangeCollector();
            var unitOfWork = new AdminRolesTestSupport.FakeUnitOfWork();
            var handler = new UpdateRolePermissionsCommandHandler(
                roleReadRepository: roleReadRepository,
                rolePermissionsRepository: rolePermissionsRepository,
                permissionKeysValidator: permissionKeysValidator,
                userRepository: userRepository,
                securityStateChangeCollector: securityStateChangeCollector,
                unitOfWork: unitOfWork);

            await handler.Handle(
                request: new UpdateRolePermissionsCommand(
                    RoleId: role.Id,
                    RolePermissionKeys: new[]
                    {
                        " users.read ",
                        "users.read",
                        "",
                        "roles.manage"
                    }),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: role.Id,
                actual: rolePermissionsRepository.RequestedRoleId);
            Assert.NotNull(permissionKeysValidator.ValidatedKeys);
            Assert.NotNull(rolePermissionsRepository.RequestedPermissionKeys);
            Assert.Equal(
                expected: new HashSet<string>(
                    collection: new[]
                    {
                        "users.read",
                        "roles.manage"
                    },
                    comparer: StringComparer.Ordinal),
                actual: permissionKeysValidator.ValidatedKeys!.ToHashSet(StringComparer.Ordinal));
            Assert.Equal(
                expected: new HashSet<string>(
                    collection: new[]
                    {
                        "users.read",
                        "roles.manage"
                    },
                    comparer: StringComparer.Ordinal),
                actual: rolePermissionsRepository.RequestedPermissionKeys!.ToHashSet(StringComparer.Ordinal));
            Assert.Equal(
                expected: role.Id,
                actual: userRepository.RequestedRoleId);
            Assert.Equal(
                expected: new[]
                {
                    firstUserId,
                    secondUserId
                },
                actual: securityStateChangeCollector.ChangedUsers);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }

        [Fact]
        public async Task Handle_WhenPermissionsDoNotChange_DoesNotMarkUsers()
        {
            Role role = AdminRolesTestSupport.CreateRole();
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
                roleReadRepository: roleReadRepository,
                rolePermissionsRepository: rolePermissionsRepository,
                permissionKeysValidator: permissionKeysValidator,
                userRepository: userRepository,
                securityStateChangeCollector: securityStateChangeCollector,
                unitOfWork: unitOfWork);

            await handler.Handle(
                request: new UpdateRolePermissionsCommand(
                    RoleId: role.Id,
                    RolePermissionKeys: new[]
                    {
                        "users.read"
                    }),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(permissionKeysValidator.ValidatedKeys);
            Assert.Equal(
                expected: role.Id,
                actual: rolePermissionsRepository.RequestedRoleId);
            Assert.Null(userRepository.RequestedRoleId);
            Assert.Empty(securityStateChangeCollector.ChangedUsers);
            Assert.Equal(
                expected: 1,
                actual: unitOfWork.TransactionCalls);
        }
    }
}
