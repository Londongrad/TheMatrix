using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessionHistoryPage;
using Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.Enums;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Sessions.GetMySessionHistoryPage
{
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
            var handler = new GetMySessionHistoryPageQueryHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                currentUser: currentUser);

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new GetMySessionHistoryPageQuery(
                        new Pagination(
                            pageNumber: 1,
                            pageSize: 10)),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.User.NotFound",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.NotFound,
                actual: exception.ErrorType);
        }

        [Fact]
        public async Task Handle_WhenUserExists_ReturnsEndedSessionsPagedAndMapped()
        {
            User user = SelfServiceHandlerTestSupport.CreateUser();
            UserSession revokedSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-3",
                deviceName: "Tablet",
                isPersistent: true);
            revokedSession.Revoke(
                reason: RefreshTokenRevocationReason.UserRevoked,
                revokedAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-2));
            UserSession expiredSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-4",
                deviceName: "Laptop",
                expiresAtUtc: SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-1),
                isPersistent: false);
            UserSession activeSession = SelfServiceHandlerTestSupport.CreateSession(
                user: user,
                deviceId: "device-5",
                deviceName: "Phone");
            var pagination = new Pagination(
                pageNumber: 1,
                pageSize: 10);
            var userRepository = new SelfServiceHandlerTestSupport.FakeUserRepository
            {
                ExistsAsyncResult = true
            };
            var userSessionRepository = new SelfServiceHandlerTestSupport.FakeUserSessionRepository
            {
                Sessions =
                {
                    revokedSession,
                    expiredSession,
                    activeSession
                }
            };
            var handler = new GetMySessionHistoryPageQueryHandler(
                userRepository: userRepository,
                userSessionRepository: userSessionRepository,
                timeProvider: SelfServiceHandlerTestSupport.CreateTimeProvider(),
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = user.Id
                });

            PagedResult<MySessionResult> result = await handler.Handle(
                request: new GetMySessionHistoryPageQuery(pagination),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: user.Id,
                actual: userRepository.RequestedUserId);
            Assert.Equal(
                expected: pagination,
                actual: userSessionRepository.RequestedPagination);
            Assert.Equal(
                expected: 2,
                actual: result.TotalCount);
            Assert.Equal(
                expected: 1,
                actual: result.PageNumber);
            Assert.Equal(
                expected: 10,
                actual: result.PageSize);
            Assert.False(result.HasPrevious);
            Assert.False(result.HasNext);

            MySessionResult[] items = result.Items.ToArray();
            Assert.Equal(
                expected: 2,
                actual: items.Length);
            Assert.Equal(
                expected: revokedSession.Id,
                actual: items[0].Id);
            Assert.False(items[0].IsActive);
            Assert.True(items[0].IsPersistent);
            Assert.Equal(
                expected: expiredSession.Id,
                actual: items[1].Id);
            Assert.False(items[1].IsActive);
            Assert.False(items[1].IsPersistent);
        }
    }
}
