using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.GetMyProfile;

public sealed class GetMyProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
    {
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository();
        var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
        {
            UserId = Guid.Parse("92000000-0000-0000-0000-000000000001")
        };
        var permissionsService = new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile.GetMyProfileQueryHandler(
            userRepository,
            currentUser,
            permissionsService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile.GetMyProfileQuery(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(currentUser.UserId, userRepository.RequestedUserId);
        Assert.Empty(permissionsService.RequestedUserIds);
    }

    [Fact]
    public async Task Handle_WhenUserDeleted_ThrowsAccountDeleted()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
        var permissionsService = new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService();
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile.GetMyProfileQueryHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            },
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id },
            permissionsService);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile.GetMyProfileQuery(),
            CancellationToken.None));

        Assert.Equal("Identity.AccountDeleted", exception.Code);
        Assert.Equal(ApplicationErrorType.Forbidden, exception.ErrorType);
        Assert.Empty(permissionsService.RequestedUserIds);
    }

    [Fact]
    public async Task Handle_WhenUserActive_ReturnsMappedProfileAndPermissions()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser(email: "neo@matrix.local", username: "neo");
        user.ChangeDisplayName("Thomas Anderson");
        user.ChangeAvatar("avatars/neo.png");
        user.RequestEmailChange(
            newEmail: Matrix.Identity.Domain.ValueObjects.Email.Create("new.neo@matrix.local"),
            requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-15));
        user.ConfirmEmail(SelfServiceHandlerTestSupport.UtcNow.AddDays(-2));
        var permissionsService = new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService
        {
            Result = new Matrix.Identity.Application.Abstractions.Services.Authorization.AuthorizationContext(
                Roles: new[] { "User" },
                Permissions: new[] { "identity.me.read", "identity.me.sessions.read" },
                PermissionsVersion: 42)
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile.GetMyProfileQueryHandler(
            new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                UserByIdWithRefreshTokens = user
            },
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id },
            permissionsService);

        var result = await handler.Handle(
            new Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile.GetMyProfileQuery(),
            CancellationToken.None);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("neo@matrix.local", result.Email);
        Assert.Equal("new.neo@matrix.local", result.PendingEmail);
        Assert.Equal("neo", result.Username);
        Assert.Equal("Thomas Anderson", result.DisplayName);
        Assert.Equal("avatars/neo.png", result.AvatarUrl);
        Assert.True(result.IsEmailConfirmed);
        Assert.Equal(user.CreatedAtUtc, result.CreatedAtUtc);
        Assert.Equal(user.EmailConfirmedAtUtc, result.EmailConfirmedAtUtc);
        Assert.Equal(new[] { "identity.me.read", "identity.me.sessions.read" }, result.EffectivePermissions);
        Assert.Equal(42, result.PermissionsVersion);
        Assert.Equal(new[] { user.Id }, permissionsService.RequestedUserIds);
    }
}
