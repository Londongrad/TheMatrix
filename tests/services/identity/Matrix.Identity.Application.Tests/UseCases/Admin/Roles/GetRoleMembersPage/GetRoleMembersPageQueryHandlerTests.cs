using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRoleMembersPage;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.GetRoleMembersPage;

public sealed class GetRoleMembersPageQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleDoesNotExist_ThrowsNotFound()
    {
        var membersReadRepository = new AdminRolesTestSupport.FakeRoleMembersReadRepository();
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        var handler = new GetRoleMembersPageQueryHandler(
            membersReadRepository,
            roleReadRepository);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new GetRoleMembersPageQuery(Guid.NewGuid(), new Pagination(pageNumber: 2, pageSize: 10)),
            CancellationToken.None));

        Assert.Equal("Identity.Role.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Null(membersReadRepository.RequestedRoleId);
    }

    [Fact]
    public async Task Handle_WhenRoleExists_ReturnsRequestedPage()
    {
        var role = AdminRolesTestSupport.CreateRole();
        var pagination = new Pagination(pageNumber: 2, pageSize: 10);
        var expectedItems = new[]
        {
            new UserListItemResult
            {
                Id = Guid.NewGuid(),
                Email = "neo@matrix.local",
                Username = "neo",
                IsEmailConfirmed = true,
                CreatedAtUtc = AdminRolesTestSupport.UtcNow.AddDays(-10)
            }
        };
        var expectedPage = new PagedResult<UserListItemResult>(
            items: expectedItems,
            totalCount: 11,
            pageNumber: pagination.PageNumber,
            pageSize: pagination.PageSize);
        var membersReadRepository = new AdminRolesTestSupport.FakeRoleMembersReadRepository
        {
            Result = expectedPage
        };
        var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
        roleReadRepository.RolesById[role.Id] = role;
        var handler = new GetRoleMembersPageQueryHandler(
            membersReadRepository,
            roleReadRepository);

        var result = await handler.Handle(
            new GetRoleMembersPageQuery(role.Id, pagination),
            CancellationToken.None);

        Assert.Equal(role.Id, roleReadRepository.ExistsRequests.Single());
        Assert.Equal(role.Id, membersReadRepository.RequestedRoleId);
        Assert.Same(pagination, membersReadRepository.RequestedPagination);
        Assert.Same(expectedPage, result);
        Assert.Single(result.Items);
    }
}
