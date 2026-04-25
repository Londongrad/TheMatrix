using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Sessions.GetMySessions;

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
        var handler = new Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions.GetUserSessionsQueryHandler(
            userRepository,
            userSessionRepository,
            new SelfServiceHandlerTestSupport.TestClock(),
            currentUser);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions.GetMySessionsQuery(),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
        Assert.Equal(currentUser.UserId, userRepository.RequestedUserId);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsActiveSessionsOrderedByLastUsage()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var newestSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-1", deviceName: "Phone");
        newestSession.Touch(
            deviceInfo: SelfServiceHandlerTestSupport.CreateDeviceInfo("device-1", "Phone", "UA-1", "203.0.113.101"),
            geoLocation: SelfServiceHandlerTestSupport.CreateGeoLocation(),
            refreshTokenExpiresAtUtc: SelfServiceHandlerTestSupport.RefreshTokenExpiresAtUtc,
            isPersistent: true,
            touchedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-1));
        var olderSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-2", deviceName: "Desktop");
        olderSession.Touch(
            deviceInfo: SelfServiceHandlerTestSupport.CreateDeviceInfo("device-2", "Desktop", "UA-2", "203.0.113.102"),
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
            Sessions = { newestSession, olderSession }
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions.GetUserSessionsQueryHandler(
            userRepository,
            userSessionRepository,
            new SelfServiceHandlerTestSupport.TestClock(),
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        var result = await handler.Handle(
            new Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions.GetMySessionsQuery(),
            CancellationToken.None);

        var items = result.ToArray();
        Assert.Equal(2, items.Length);
        Assert.Equal(newestSession.Id, items[0].Id);
        Assert.Equal("device-1", items[0].DeviceId);
        Assert.Equal("Phone", items[0].DeviceName);
        Assert.Equal("UA-1", items[0].UserAgent);
        Assert.Equal("203.0.113.101", items[0].IpAddress);
        Assert.True(items[0].IsActive);
        Assert.True(items[0].IsPersistent);
        Assert.Equal(olderSession.Id, items[1].Id);
        Assert.Equal("device-2", items[1].DeviceId);
        Assert.False(items[1].IsPersistent);
        Assert.True(items[1].IsActive);
    }
}
