using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolePermissions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.GetRolePermissions;

public sealed class GetRolePermissionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleDoesNotExist_ThrowsNotFound()
    {
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository();
        var handler = new GetRolePermissionsQueryHandler(
            roleReadRepository,
            rolePermissionsRepository);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new GetRolePermissionsQuery(Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal("Identity.Role.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Null(rolePermissionsRepository.RequestedRoleId);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_ReturnsRolePermissions()
    {
        var role = AdminRolesTestSupport.CreateRole();
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        roleReadRepository.RolesById[role.Id] = role;
        var rolePermissionsRepository = new AdminRolesTestSupport.FakeRolePermissionsRepository
        {
            GetRolePermissionsResult = new[] { "users.read", "roles.manage" }
        };
        var handler = new GetRolePermissionsQueryHandler(
            roleReadRepository,
            rolePermissionsRepository);

        var result = await handler.Handle(
            new GetRolePermissionsQuery(role.Id),
            CancellationToken.None);

        Assert.Equal(role.Id, roleReadRepository.ExistsRequests.Single());
        Assert.Equal(role.Id, rolePermissionsRepository.RequestedRoleId);
        Assert.Equal(new[] { "users.read", "roles.manage" }, result);
    }
}
