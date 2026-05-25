using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Sessions.GetMySessions
{
    public sealed class GetMySessionsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenUserMissing_ThrowsUserNotFound()
        {
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = false
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository();
            var currentUser = new SelfServiceHandlerTestSupport.FakeCurrentUserContext
            {
                UserId = Guid.Parse("92000000-0000-0000-0000-000000000002")
            };
            var handler = new GetUserSessionsQueryHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                currentUser: currentUser);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GetMySessionsQuery(),
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
        }

        [Fact]
        public async Task Handle_WhenUserExists_ReturnsActiveSessionsOrderedByLastUsage()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            UserSession newestSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-1",
                deviceName: "Phone");
            newestSession.Touch(
                deviceInfo: SelfServiceHandlerTestSupport.CreateDeviceInfo(
                    deviceId: "device-1",
                    deviceName: "Phone",
                    userAgent: "UA-1",
                    ipAddress: "203.0.113.101"),
                geoLocation: SelfServiceHandlerTestSupport.CreateGeoLocation(),
                refreshTokenExpiresAtUtc: SelfServiceHandlerTestSupport.RefreshTokenExpiresAtUtc,
                isPersistent: true,
                touchedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-1));
            UserSession olderSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-2",
                deviceName: "Desktop");
            olderSession.Touch(
                deviceInfo: SelfServiceHandlerTestSupport.CreateDeviceInfo(
                    deviceId: "device-2",
                    deviceName: "Desktop",
                    userAgent: "UA-2",
                    ipAddress: "203.0.113.102"),
                geoLocation: SelfServiceHandlerTestSupport.CreateGeoLocation(),
                refreshTokenExpiresAtUtc: SelfServiceHandlerTestSupport.RefreshTokenExpiresAtUtc,
                isPersistent: false,
                touchedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-10));
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = true
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
            {
                Sessions =
                {
                    newestSession,
                    olderSession
                }
            };
            var handler = new GetUserSessionsQueryHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            IReadOnlyCollection<MySessionResult> result = await handler.Handle(
                request: new GetMySessionsQuery(),
                cancellationToken: CancellationToken.None);

            MySessionResult[] items = result.ToArray();
            Assert.Equal(
                expected: 2,
                actual: items.Length);
            Assert.Equal(
                expected: newestSession.Id,
                actual: items[0].Id);
            Assert.Equal(
                expected: "device-1",
                actual: items[0].DeviceId);
            Assert.Equal(
                expected: "Phone",
                actual: items[0].DeviceName);
            Assert.Equal(
                expected: "UA-1",
                actual: items[0].UserAgent);
            Assert.Equal(
                expected: "203.0.113.101",
                actual: items[0].IpAddress);
            Assert.True(items[0].IsActive);
            Assert.True(items[0].IsPersistent);
            Assert.Equal(
                expected: olderSession.Id,
                actual: items[1].Id);
            Assert.Equal(
                expected: "device-2",
                actual: items[1].DeviceId);
            Assert.False(items[1].IsPersistent);
            Assert.True(items[1].IsActive);
        }
    }
}
