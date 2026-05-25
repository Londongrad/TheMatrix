using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolePermissions;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.GetRolePermissions
{
    public sealed class GetRolePermissionsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenRoleDoesNotExist_ThrowsNotFound()
        {
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository();
            var handler = new GetRolePermissionsQueryHandler(
                roleReadRepository: roleReadRepository,
                rolePermissionsRepository: rolePermissionsRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GetRolePermissionsQuery(Guid.NewGuid()),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Null(rolePermissionsRepository.RequestedRoleId);
        }

        [Fact]
        public async Task Handle_WhenRoleExists_ReturnsRolePermissions()
        {
            Role role = AdminRolesTestSupport.CreateRole();
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            roleReadRepository.RolesById[role.Id] = role;
            var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository
            {
                GetRolePermissionsResult = new[]
                {
                    "users.read",
                    "roles.manage"
                }
            };
            var handler = new GetRolePermissionsQueryHandler(
                roleReadRepository: roleReadRepository,
                rolePermissionsRepository: rolePermissionsRepository);

            IReadOnlyCollection<string> result = await handler.Handle(
                request: new GetRolePermissionsQuery(role.Id),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: role.Id,
                actual: roleReadRepository.ExistsRequests.Single());
            Assert.Equal(
                expected: role.Id,
                actual: rolePermissionsRepository.RequestedRoleId);
            Assert.Equal(
                expected: new[]
                {
                    "users.read",
                    "roles.manage"
                },
                actual: result);
        }
    }
}
