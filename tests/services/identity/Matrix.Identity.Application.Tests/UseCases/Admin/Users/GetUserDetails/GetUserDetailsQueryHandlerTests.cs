using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserDetails;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUserDetails;

public sealed class GetUserDetailsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserDoesNotExist_ThrowsNotFound()
    {
        var userRepository = new AdminUsersTestSupport.FakeUserRepository();
        var userSessionRepository = new AdminUsersTestSupport.FakeUserSessionRepository();
        var handler = new GetUserDetailsQueryHandler(userRepository, userSessionRepository);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new GetUserDetailsQuery(Guid.NewGuid()),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Null(userSessionRepository.RequestedUserId);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsMappedDetails()
    {
        var user = AdminUsersTestSupport.CreateUser();
        user.ChangeAvatar("avatars/neo.png");
        user.ConfirmEmail(AdminUsersTestSupport.UtcNow.AddDays(-5));
        user.BumpPermissionsVersion();
        var session = AdminUsersTestSupport.CreateSession(user);
        session.Touch(
            deviceInfo: DeviceInfo.Create("device-1", "Phone", "Mozilla/5.0", "127.0.0.1"),
            geoLocation: GeoLocation.Create("Russia", "Zabaykalsky Krai", "Chita"),
            refreshTokenExpiresAtUtc: AdminUsersTestSupport.RefreshTokenExpiresAtUtc,
            isPersistent: true,
            touchedAtUtc: AdminUsersTestSupport.UtcNow.AddMinutes(-30));
        var userRepository = new AdminUsersTestSupport.FakeUserRepository
        {
            UserById = user
        };
        var userSessionRepository = new AdminUsersTestSupport.FakeUserSessionRepository();
        userSessionRepository.Sessions.Add(session);
        var handler = new GetUserDetailsQueryHandler(userRepository, userSessionRepository);

        var result = await handler.Handle(new GetUserDetailsQuery(user.Id), CancellationToken.None);

        Assert.Equal(user.Id, userRepository.RequestedUserId);
        Assert.Equal(user.Id, userSessionRepository.RequestedUserId);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("avatars/neo.png", result.AvatarUrl);
        Assert.Equal("neo", result.Username);
        Assert.Equal("neo@matrix.local", result.Email);
        Assert.True(result.IsEmailConfirmed);
        Assert.False(result.IsLocked);
        Assert.False(result.IsDeleted);
        Assert.Equal(user.PermissionsVersion, result.PermissionsVersion);
        Assert.Equal(user.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(AdminUsersTestSupport.UtcNow.AddMinutes(-30), result.LastVisitedAtUtc);
    }
}
