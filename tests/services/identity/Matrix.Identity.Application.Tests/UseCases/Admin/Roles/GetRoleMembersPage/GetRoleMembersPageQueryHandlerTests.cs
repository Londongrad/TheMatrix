using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Admin.Roles.GetRoleMembersPage;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.GetRoleMembersPage
{
    public sealed class GetRoleMembersPageQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenRoleDoesNotExist_ThrowsNotFound()
        {
            var membersReadRepository = new AdminRolesTestSupport.FakeRoleMembersReadRepository();
            var roleReadRepository = new AdminRolesTestSupport.FakeRoleReadRepository();
            var handler = new GetRoleMembersPageQueryHandler(
                membersReadRepository: membersReadRepository,
                roleReadRepository: roleReadRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GetRoleMembersPageQuery(
                        RoleId: Guid.NewGuid(),
                        Pagination: new Pagination(
                            pageNumber: 2,
                            pageSize: 10)),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.Role.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Null(membersReadRepository.RequestedRoleId);
        }

        [Fact]
        public async Task Handle_WhenRoleExists_ReturnsRequestedPage()
        {
            Role role = AdminRolesTestSupport.CreateRole();
            var pagination = new Pagination(
                pageNumber: 2,
                pageSize: 10);
            UserListItemResult[] expectedItems = new[]
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
                membersReadRepository: membersReadRepository,
                roleReadRepository: roleReadRepository);

            PagedResult<UserListItemResult> result = await handler.Handle(
                request: new GetRoleMembersPageQuery(
                    RoleId: role.Id,
                    Pagination: pagination),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: role.Id,
                actual: roleReadRepository.ExistsRequests.Single());
            Assert.Equal(
                expected: role.Id,
                actual: membersReadRepository.RequestedRoleId);
            Assert.Same(
                expected: pagination,
                actual: membersReadRepository.RequestedPagination);
            Assert.Same(
                expected: expectedPage,
                actual: result);
            Assert.Single(result.Items);
        }
    }
}
