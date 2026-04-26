using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUserRoles;

public sealed class GetUserRolesQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenRolesExist_ReturnsRolesWithoutExistenceCheck()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository();
        var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository
        {
            GetUserRolesResult =
            [
                new UserRoleResult
                {
                    Id = Guid.NewGuid(),
                    Name = "Operators",
                    IsSystem = false,
                    CreatedAtUtc = AdminUsersTestSupport.UtcNow.AddDays(-4)
                }
            ]
        };
        var handler = new GetUserRolesQueryHandler(userRepository, userRolesRepository);

        var result = await handler.Handle(new GetUserRolesQuery(userId), CancellationToken.None);

        Assert.Equal(userId, userRolesRepository.RequestedUserId);
        Assert.Null(userRepository.RequestedUserId);
        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoRolesAndDoesNotExist_ThrowsNotFound()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = false
        };
        var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository();
        var handler = new GetUserRolesQueryHandler(userRepository, userRolesRepository);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new GetUserRolesQuery(userId),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(userId, userRepository.RequestedUserId);
    }

    [Fact]
    public async Task Handle_WhenUserExistsAndHasNoRoles_ReturnsEmptyCollection()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var userRolesRepository = new AdminUsersTestSupport.FakeUserRolesRepository();
        var handler = new GetUserRolesQueryHandler(userRepository, userRolesRepository);

        var result = await handler.Handle(new GetUserRolesQuery(userId), CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(userId, userRepository.RequestedUserId);
    }
}
