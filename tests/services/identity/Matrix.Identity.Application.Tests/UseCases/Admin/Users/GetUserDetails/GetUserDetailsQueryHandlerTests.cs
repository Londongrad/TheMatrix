using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Admin.Users.GetUserDetails;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Users.GetUserDetails
{
    public sealed class GetUserDetailsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserDoesNotExist_ThrowsNotFound()
        {
            var userRepository = new AdminUsersTestSupport.FakeUserRepository();
            var userSessionRepository = new AdminUsersTestSupport.FakeUserSessionRepository();
            var handler = new GetUserDetailsQueryHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GetUserDetailsQuery(Guid.NewGuid()),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
            Assert.Null(userSessionRepository.RequestedUserId);
        }

        [Fact]
        public async Task Handle_WhenUserExists_ReturnsMappedDetails()
        {
            User user = AdminUsersTestSupport.CreateUser();
            user.ChangeAvatar("avatars/neo.png");
            user.ConfirmEmail(AdminUsersTestSupport.UtcNow.AddDays(-5));
            user.BumpPermissionsVersion();
            UserSession session = AdminUsersTestSupport.CreateSession(user);
            session.Touch(
                deviceInfo: DeviceInfo.Create(
                    deviceId: "device-1",
                    deviceName: "Phone",
                    userAgent: "Mozilla/5.0",
                    ipAddress: "127.0.0.1"),
                geoLocation: GeoLocation.Create(
                    country: "Russia",
                    region: "Zabaykalsky Krai",
                    city: "Chita"),
                refreshTokenExpiresAtUtc: AdminUsersTestSupport.RefreshTokenExpiresAtUtc,
                isPersistent: true,
                touchedAtUtc: AdminUsersTestSupport.UtcNow.AddMinutes(-30));
            var userRepository = new AdminUsersTestSupport.FakeUserRepository
            {
                UserById = user
            };
            var userSessionRepository = new AdminUsersTestSupport.FakeUserSessionRepository();
            userSessionRepository.Sessions.Add(session);
            var handler = new GetUserDetailsQueryHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository);

            UserDetailsResult result = await handler.Handle(
                request: new GetUserDetailsQuery(user.Id),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: user.Id,
                actual: userRepository.RequestedUserId);
            Assert.Equal(
                expected: user.Id,
                actual: userSessionRepository.RequestedUserId);
            Assert.Equal(
                expected: user.Id,
                actual: result.Id);
            Assert.Equal(
                expected: "avatars/neo.png",
                actual: result.AvatarUrl);
            Assert.Equal(
                expected: "neo",
                actual: result.Username);
            Assert.Equal(
                expected: "neo@matrix.local",
                actual: result.Email);
            Assert.True(result.IsEmailConfirmed);
            Assert.False(result.IsLocked);
            Assert.False(result.IsDeleted);
            Assert.Equal(
                expected: user.PermissionsVersion,
                actual: result.PermissionsVersion);
            Assert.Equal(
                expected: user.CreatedAtUtc,
                actual: result.CreatedAtUtc);
            Assert.Equal(
                expected: AdminUsersTestSupport.UtcNow.AddMinutes(-30),
                actual: result.LastVisitedAtUtc);
        }
    }
}
