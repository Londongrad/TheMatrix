using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserPermissions;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUserPermissions;

public sealed class GetUserPermissionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenOverridesExist_ReturnsOverridesWithoutExistenceCheck()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository();
        var permissionsRepository = new AdminUsersTestSupport.FakeUserPermissionsRepository
        {
            GetUserPermissionsResult =
            [
                new UserPermissionOverrideResult
                {
                    PermissionKey = "users.read",
                    Effect = PermissionEffect.Allow
                }
            ]
        };
        var handler = new GetUserPermissionsQueryHandler(userRepository, permissionsRepository);

        var result = await handler.Handle(new GetUserPermissionsQuery(userId), CancellationToken.None);

        Assert.Equal(userId, permissionsRepository.RequestedUserId);
        Assert.Null(userRepository.RequestedUserId);
        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_WhenOverridesEmptyAndUserDoesNotExist_ThrowsNotFound()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = false
        };
        var permissionsRepository = new AdminUsersTestSupport.FakeUserPermissionsRepository();
        var handler = new GetUserPermissionsQueryHandler(userRepository, permissionsRepository);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new GetUserPermissionsQuery(userId),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(userId, userRepository.RequestedUserId);
    }

    [Fact]
    public async Task Handle_WhenUserExistsAndOverridesEmpty_ReturnsEmptyCollection()
    {
        Guid userId = Guid.NewGuid();
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var permissionsRepository = new AdminUsersTestSupport.FakeUserPermissionsRepository();
        var handler = new GetUserPermissionsQueryHandler(userRepository, permissionsRepository);

        var result = await handler.Handle(new GetUserPermissionsQuery(userId), CancellationToken.None);

        Assert.Empty(result);
        Assert.Equal(userId, userRepository.RequestedUserId);
    }
}
