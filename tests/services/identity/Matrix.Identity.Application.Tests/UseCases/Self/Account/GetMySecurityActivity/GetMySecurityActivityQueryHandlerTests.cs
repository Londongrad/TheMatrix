using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Application.Abstractions.Services.Security;
using Xunit;
using ProdSecurityActivity = Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed class GetMySecurityActivityQueryHandlerTests
    {
        [Fact]
        public async Task Handle_WhenCursorMissing_PassesNullCursorToRepository()
        {
            var userId = Guid.Parse("93000000-0000-0000-0000-000000000001");
            var firstEventId = Guid.Parse("93000000-0000-0000-0000-000000000010");
            var repository = new SelfServiceHandlerTestSupport.FakeSecurityAuditReadRepository
            {
                Result = new CursorPagedResult<ProdSecurityActivity.SecurityActivityItemResult>(
                    items:
                    [
                        new ProdSecurityActivity.SecurityActivityItemResult
                        {
                            EventId = firstEventId,
                            EventType = SecurityAuditEventType.Login,
                            IsSuccessful = true,
                            OccurredAtUtc = SelfServiceHandlerTestSupport.UtcNow.AddMinutes(-30),
                            IpAddress = "203.0.113.120",
                            UserAgent = "Mozilla/5.0",
                            DeviceId = "device-1",
                            DeviceName = "Phone",
                            Details = null
                        }
                    ],
                    pageSize: 25,
                    nextCursor: "next-cursor")
            };
            var handler = new ProdSecurityActivity.GetMySecurityActivityQueryHandler(
                securityAuditReadRepository: repository,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = userId
                });

            CursorPagedResult<ProdSecurityActivity.SecurityActivityItemResult> result = await handler.Handle(
                request: new ProdSecurityActivity.GetMySecurityActivityQuery(
                    Cursor: null,
                    PageSize: 25),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: (userId, null, 25),
                actual: repository.Request);
            Assert.Single(result.Items);
            Assert.True(result.HasNext);
            Assert.Equal(
                expected: "next-cursor",
                actual: result.NextCursor);
        }

        [Fact]
        public async Task Handle_WhenCursorValid_DecodesCursorAndPassesItToRepository()
        {
            var userId = Guid.Parse("93000000-0000-0000-0000-000000000002");
            var expectedCursor = new ProdSecurityActivity.SecurityActivityCursor(
                UtcTicks: SelfServiceHandlerTestSupport.UtcNow.AddHours(-1)
                   .Ticks,
                EventId: Guid.Parse("93000000-0000-0000-0000-000000000011"));
            string encodedCursor = ProdSecurityActivity.SecurityActivityCursorCodec.Encode(expectedCursor);
            var repository = new SelfServiceHandlerTestSupport.FakeSecurityAuditReadRepository
            {
                Result = new CursorPagedResult<ProdSecurityActivity.SecurityActivityItemResult>(
                    items: Array.Empty<ProdSecurityActivity.SecurityActivityItemResult>(),
                    pageSize: 10,
                    nextCursor: null)
            };
            var handler = new ProdSecurityActivity.GetMySecurityActivityQueryHandler(
                securityAuditReadRepository: repository,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = userId
                });

            CursorPagedResult<ProdSecurityActivity.SecurityActivityItemResult> result = await handler.Handle(
                request: new ProdSecurityActivity.GetMySecurityActivityQuery(
                    Cursor: encodedCursor,
                    PageSize: 10),
                cancellationToken: CancellationToken.None);

            (Guid UserId, ProdSecurityActivity.SecurityActivityCursor? Cursor, int PageSize) request =
                Assert.IsType<(Guid UserId, ProdSecurityActivity.SecurityActivityCursor? Cursor, int PageSize)>(
                    repository.Request!.Value);
            Assert.Equal(
                expected: userId,
                actual: request.UserId);
            Assert.Equal(
                expected: expectedCursor,
                actual: request.Cursor);
            Assert.Equal(
                expected: 10,
                actual: request.PageSize);
            Assert.Empty(result.Items);
            Assert.False(result.HasNext);
            Assert.Null(result.NextCursor);
        }

        [Fact]
        public async Task Handle_WhenCursorInvalid_ThrowsValidationError()
        {
            var repository = new SelfServiceHandlerTestSupport.FakeSecurityAuditReadRepository();
            var handler = new ProdSecurityActivity.GetMySecurityActivityQueryHandler(
                securityAuditReadRepository: repository,
                currentUser: new SelfServiceHandlerTestSupport.FakeCurrentUserContext
                {
                    UserId = Guid.Parse("93000000-0000-0000-0000-000000000003")
                });

            MatrixApplicationException exception = await Assert.ThrowsAsync<MatrixApplicationException>(()
                => handler.Handle(
                    request: new ProdSecurityActivity.GetMySecurityActivityQuery(
                        Cursor: "not-a-valid-cursor",
                        PageSize: 15),
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: "Identity.SecurityActivity.InvalidCursor",
                actual: exception.Code);
            Assert.Equal(
                expected: ApplicationErrorType.Validation,
                actual: exception.ErrorType);
            Assert.Null(repository.Request);
        }
    }
}
