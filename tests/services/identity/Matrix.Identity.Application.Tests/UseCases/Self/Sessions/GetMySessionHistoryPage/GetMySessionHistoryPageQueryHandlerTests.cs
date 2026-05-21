using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Tests.UseCases.Self;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Sessions.GetMySessionHistoryPage;

public sealed class GetMySessionHistoryPageQueryHandlerTests
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
            UserId = Guid.Parse("92000000-0000-0000-0000-000000000003")
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessionHistoryPage.GetMySessionHistoryPageQueryHandler(
            userRepository,
            userSessionRepository,
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            currentUser);

        var exception = await Assert.ThrowsAsync<MatrixApplicationException>(() => handler.Handle(
            new Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessionHistoryPage.GetMySessionHistoryPageQuery(new Pagination(1, 10)),
            CancellationToken.None));

        Assert.Equal("Identity.User.NotFound", exception.Code);
        Assert.Equal(ApplicationErrorType.NotFound, exception.ErrorType);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsEndedSessionsPagedAndMapped()
    {
        var user = SelfServiceHandlerTestSupport.CreateUser();
        var revokedSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-3", deviceName: "Tablet", isPersistent: true);
        revokedSession.Revoke(RefreshTokenRevocationReason.UserRevoked, SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-2));
        var expiredSession = SelfServiceHandlerTestSupport.CreateSession(
            user,
            deviceId: "device-4",
            deviceName: "Laptop",
            expiresAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-1),
            isPersistent: false);
        var activeSession = SelfServiceHandlerTestSupport.CreateSession(user, deviceId: "device-5", deviceName: "Phone");
        var pagination = new Pagination(1, 10);
        var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
        {
            ExistsAsyncResult = true
        };
        var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
        {
            Sessions = { revokedSession, expiredSession, activeSession }
        };
        var handler = new Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessionHistoryPage.GetMySessionHistoryPageQueryHandler(
            userRepository,
            userSessionRepository,
            SelfServiceHandlerTestSupport.CreateTimeProvider(),
            new SelfServiceHandlerTestSupport.FakeCurrentUserContext { UserId = user.Id });

        var result = await handler.Handle(
            new Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessionHistoryPage.GetMySessionHistoryPageQuery(pagination),
            CancellationToken.None);

        Assert.Equal(user.Id, userRepository.RequestedUserId);
        Assert.Equal(pagination, userSessionRepository.RequestedPagination);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.False(result.HasPrevious);
        Assert.False(result.HasNext);

        var items = result.Items.ToArray();
        Assert.Equal(2, items.Length);
        Assert.Equal(revokedSession.Id, items[0].Id);
        Assert.False(items[0].IsActive);
        Assert.True(items[0].IsPersistent);
        Assert.Equal(expiredSession.Id, items[1].Id);
        Assert.False(items[1].IsActive);
        Assert.False(items[1].IsPersistent);
    }
}
