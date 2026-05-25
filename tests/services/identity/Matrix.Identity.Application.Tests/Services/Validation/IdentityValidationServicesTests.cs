using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Services;
using Matrix.Identity.Application.Tests.UseCases.Admin.Roles;
using Matrix.Identity.Application.Tests.UseCases.Admin.Users;
using Matrix.Identity.Application.UseCases.Admin.Permissions.GetPermissionsCatalog;
using Xunit;

namespace Matrix.Identity.Application.Tests.Services.Validation
{
    public sealed class IdentityValidationServicesTests
    {
        [Fact]
        public async Task PermissionKeysValidator_WhenPermissionIsMissing_ThrowsValidationError()
        {
            var repository = new AdminUsersTestSupport.FakePermissionReadRepository
            {
                Permissions =
                [
                    new PermissionCatalogItemResult
                    {
                        Key = "users.read",
                        Service = "Identity",
                        Group = "Users",
                        Description = "Read users",
                        IsDeprecated = false
                    }
                ]
            };
            var validator = new PermissionKeysValidator(repository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => validator.ValidateAsync(
                    permissionKeys:
                    [
                        "users.read",
                        "roles.write"
                    ],
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.ValidationFailed",
                actual: exception.Code);
            Assert.NotNull(exception.Errors);
            Assert.Equal(
                expected: "Permissions not found: roles.write",
                actual: Assert.Single(exception.Errors!["permissionKeys"]));
        }

        [Fact]
        public async Task PermissionKeysValidator_WhenPermissionIsDeprecated_ThrowsValidationError()
        {
            var repository = new AdminUsersTestSupport.FakePermissionReadRepository
            {
                Permissions =
                [
                    new PermissionCatalogItemResult
                    {
                        Key = "users.read",
                        Service = "Identity",
                        Group = "Users",
                        Description = "Read users",
                        IsDeprecated = false
                    },
                    new PermissionCatalogItemResult
                    {
                        Key = "legacy.write",
                        Service = "Identity",
                        Group = "Legacy",
                        Description = "Legacy write",
                        IsDeprecated = true
                    }
                ]
            };
            var validator = new PermissionKeysValidator(repository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => validator.ValidateAsync(
                    permissionKeys:
                    [
                        "users.read",
                        "legacy.write"
                    ],
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.ValidationFailed",
                actual: exception.Code);
            Assert.NotNull(exception.Errors);
            Assert.Equal(
                expected: "Deprecated permissions: legacy.write",
                actual: Assert.Single(exception.Errors!["permissionKeys"]));
        }

        [Fact]
        public async Task PermissionKeysValidator_WhenAllPermissionsAreKnownAndActive_Completes()
        {
            var repository = new AdminUsersTestSupport.FakePermissionReadRepository
            {
                Permissions =
                [
                    new PermissionCatalogItemResult
                    {
                        Key = "users.read",
                        Service = "Identity",
                        Group = "Users",
                        Description = "Read users",
                        IsDeprecated = false
                    },
                    new PermissionCatalogItemResult
                    {
                        Key = "roles.write",
                        Service = "Identity",
                        Group = "Roles",
                        Description = "Write roles",
                        IsDeprecated = false
                    }
                ]
            };
            var validator = new PermissionKeysValidator(repository);

            await validator.ValidateAsync(
                permissionKeys:
                [
                    "users.read",
                    "roles.write"
                ],
                cancellationToken: CancellationToken.None);
        }

        [Fact]
        public async Task RoleIdsValidator_WhenRoleIdIsMissing_ThrowsValidationError()
        {
            var existingRoleId = Guid.NewGuid();
            var missingRoleId = Guid.NewGuid();
            var repository = new AdminRolesTestSupport.FakeRoleReadRepository();
            repository.RolesById[existingRoleId] = AdminRolesTestSupport.CreateRole();
            var validator = new RoleIdsValidator(repository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => validator.ValidateExistAsync(
                    roleIds:
                    [
                        existingRoleId,
                        missingRoleId
                    ],
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.ValidationFailed",
                actual: exception.Code);
            Assert.NotNull(exception.Errors);
            Assert.Contains(
                expectedSubstring: missingRoleId.ToString(),
                actualString: Assert.Single(exception.Errors!["roleIds"]));
        }

        [Fact]
        public async Task RoleIdsValidator_WhenAllRoleIdsExist_Completes()
        {
            var firstRoleId = Guid.NewGuid();
            var secondRoleId = Guid.NewGuid();
            var repository = new AdminRolesTestSupport.FakeRoleReadRepository();
            repository.RolesById[firstRoleId] = AdminRolesTestSupport.CreateRole();
            repository.RolesById[secondRoleId] = AdminRolesTestSupport.CreateRole("Moderators");
            var validator = new RoleIdsValidator(repository);

            await validator.ValidateExistAsync(
                roleIds:
                [
                    firstRoleId,
                    secondRoleId
                ],
                cancellationToken: CancellationToken.None);
        }
    }
}
