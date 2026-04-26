using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUsersPage;

public sealed class GetUsersPageQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsPagedUsersFromRepository()
    {
        var pagination = new Pagination(pageNumber: 2, pageSize: 10);
        var expectedPage = new PagedResult<UserListItemResult>(
            items:
            [
                new UserListItemResult
                {
                    Id = Guid.NewGuid(),
                    Email = "neo@matrix.local",
                    Username = "neo",
                    IsEmailConfirmed = true,
                    CreatedAtUtc = AdminUsersTestSupport.UtcNow.AddDays(-5)
                }
            ],
            totalCount: 11,
            pageNumber: pagination.PageNumber,
            pageSize: pagination.PageSize);
        var userRepository = new AdminUsersTestSupport.FakeUserAdminReadRepository
        {
            Result = expectedPage
        };
        var handler = new GetUsersPageQueryHandler(userRepository);

        var result = await handler.Handle(new GetUsersPageQuery(pagination), CancellationToken.None);

        Assert.Same(pagination, userRepository.RequestedPagination);
        Assert.Same(expectedPage, result);
        Assert.Single(result.Items);
        Assert.Equal(11, result.TotalCount);
    }
}
