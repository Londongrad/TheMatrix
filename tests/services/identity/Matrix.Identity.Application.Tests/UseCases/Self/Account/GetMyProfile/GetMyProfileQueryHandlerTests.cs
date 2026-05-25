using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Abstractions.Services.Authorization;
using Matrix.Identity.Application.UseCases.Self.Account.GetMyProfile;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.GetMyProfile
{
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
            var handler = new GetMyProfileQueryHandler(
                userRepository: userRepository,
                currentUser: currentUser,
                permissionsService: permissionsService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GetMyProfileQuery(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Equal(
                expected: currentUser.UserId,
                actual: userRepository.RequestedUserId);
            Assert.Empty(permissionsService.RequestedUserIds);
        }

        [Fact]
        public async Task Handle_WhenUserDeleted_ThrowsAccountDeleted()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(isDeleted: true);
            var permissionsService = new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService();
            var handler = new GetMyProfileQueryHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserByIdWithRefreshTokens = user
                },
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                },
                permissionsService: permissionsService);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GetMyProfileQuery(),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.AccountDeleted",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Forbidden,
                actual: exception.ErrorType);
            Assert.Empty(permissionsService.RequestedUserIds);
        }

        [Fact]
        public async Task Handle_WhenUserActive_ReturnsMappedProfileAndPermissions()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser(
                email: "neo@matrix.local",
                username: "neo");
            user.ChangeDisplayName("Thomas Anderson");
            user.ChangeAvatar("avatars/neo.png");
            user.RequestEmailChange(
                newEmail: Email.Create("new.neo@matrix.local"),
                requestedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-15));
            user.ConfirmEmail(SelfServiceHandlerTestSupport.UtcNow.AddDays(-2));
            var permissionsService = new SelfServiceHandlerTestSupport.FakeEffectivePermissionsService
            {
                Result = new AuthorizationContext(
                    Roles: new[]
                    {
                        "User"
                    },
                    Permissions: new[]
                    {
                        "identity.me.read",
                        "identity.me.sessions.read"
                    },
                    PermissionsVersion: 42)
            };
            var handler = new GetMyProfileQueryHandler(
                userRepository: new SelfServiceHandlerTestSupport.FakeUserRepository
                {
                    UserByIdWithRefreshTokens = user
                },
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                },
                permissionsService: permissionsService);

            MyProfileResult result = await handler.Handle(
                request: new GetMyProfileQuery(),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: user.Id,
                actual: result.UserId);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: result.Email);
            Assert.Equal(
                expected: "new.neo@matrix.local",
                actual: result.PendingEmail);
            Assert.Equal(
                expected: "neo",
                actual: result.Username);
            Assert.Equal(
                expected: "Thomas Anderson",
                actual: result.DisplayName);
            Assert.Equal(
                expected: "avatars/neo.png",
                actual: result.AvatarUrl);
            Assert.True(result.IsEmailConfirmed);
            Assert.Equal(
                expected: user.CreatedAtUtc,
                actual: result.CreatedAtUtc);
            Assert.Equal(
                expected: user.EmailConfirmedAtUtc,
                actual: result.EmailConfirmedAtUtc);
            Assert.Equal(
                expected: new[]
                {
                    "identity.me.read",
                    "identity.me.sessions.read"
                },
                actual: result.EffectivePermissions);
            Assert.Equal(
                expected: 42,
                actual: result.PermissionsVersion);
            Assert.Equal(
                expected: new[]
                {
                    user.Id
                },
                actual: permissionsService.RequestedUserIds);
        }
    }
}
