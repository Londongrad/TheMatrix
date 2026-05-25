using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUsersPage
{
    public sealed class GetUsersPageQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsPagedUsersFromRepository()
        {
            var pagination = new Pagination(
                pageNumber: 2,
                pageSize: 10);
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

            PagedResult<UserListItemResult> result = await handler.Handle(
                request: new GetUsersPageQuery(pagination),
                cancellationToken: CancellationToken.None);

            Assert.Same(
                expected: pagination,
                actual: userRepository.RequestedPagination);
            Assert.Same(
                expected: expectedPage,
                actual: result);
            Assert.Single(result.Items);
            Assert.Equal(
                expected: 11,
                actual: result.TotalCount);
        }
    }
}
