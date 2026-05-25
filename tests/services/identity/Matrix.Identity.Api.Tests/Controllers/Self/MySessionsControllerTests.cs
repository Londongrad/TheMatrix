using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Api.Controllers.Self;
using Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessionHistoryPage;
using Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessions;
using Matrix.Identity.Application.UseCases.Self.Sessions.RevokeMySession;
using Matrix.Identity.Application.UseCases.Self.Sessions.RevokeOtherMySessions;
using Matrix.Identity.Contracts.Self.Sessions.Responses;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.IdentityApiTestSupport;

namespace Matrix.Identity.Api.Tests.Controllers.Self
{
    public sealed class MySessionsControllerTests
    {
        [Fact]
        public async Task GetSessions_MapsCurrentSessionFromClaim()
        {
            var currentSessionId = Guid.Parse("846bc0af-f5d0-434b-a7e0-a285dbe2b3a5");
            var otherSessionId = Guid.Parse("63191635-397a-4fd3-9697-4fe7d6b44b5a");
            var sender = new FakeSender();
            sender.Handle<GetMySessionsQuery, IReadOnlyCollection<MySessionResult>>(_ =>
            [
                CreateMySessionResult(
                    sessionId: currentSessionId,
                    isActive: true),
                CreateMySessionResult(
                    sessionId: otherSessionId,
                    isActive: false,
                    isPersistent: false,
                    ipAddress: null)
            ]);
            MySessionsController controller = AttachHttpContext(
                controller: new MySessionsController(sender),
                httpContext: CreateHttpContext(
                    path: "/api/me/sessions",
                    userId: Guid.Parse("16c09f15-d5cd-49ee-981c-ab628e60ba61"),
                    sessionId: currentSessionId));

            ActionResult<List<SessionResponse>> actionResult = await controller.GetSessions(CancellationToken.None);

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            List<SessionResponse> response = Assert.IsType<List<SessionResponse>>(ok.Value);

            Assert.Equal(
                expected: 2,
                actual: response.Count);
            Assert.True(response[0].IsCurrent);
            Assert.True(response[0].IsActive);
            Assert.False(response[1].IsCurrent);
            Assert.False(response[1].IsActive);
            Assert.False(response[1].IsPersistent);
        }

        [Fact]
        public async Task GetSessionHistoryPage_MapsPaginationAndClearsCurrentFlags()
        {
            var sessionId = Guid.Parse("15ae2a1c-5002-4129-b6d6-1d21eaf5274e");
            var sender = new FakeSender();
            sender.Handle<GetMySessionHistoryPageQuery, PagedResult<MySessionResult>>(query =>
                new PagedResult<MySessionResult>(
                    items:
                    [
                        CreateMySessionResult(
                            sessionId: sessionId,
                            isActive: true)
                    ],
                    totalCount: 11,
                    pageNumber: query.Pagination.PageNumber,
                    pageSize: query.Pagination.PageSize));
            MySessionsController controller = AttachHttpContext(
                controller: new MySessionsController(sender),
                httpContext: CreateHttpContext(
                    path: "/api/me/sessions/history",
                    userId: Guid.Parse("fd23336d-245b-442f-a1dc-bf576e3348b6"),
                    sessionId: sessionId));

            ActionResult<PagedResult<SessionResponse>> actionResult = await controller.GetSessionHistoryPage(
                pageNumber: 2,
                pageSize: 25,
                cancellationToken: default(CancellationToken));

            OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            PagedResult<SessionResponse> response = Assert.IsType<PagedResult<SessionResponse>>(ok.Value);
            GetMySessionHistoryPageQuery query = Assert.IsType<GetMySessionHistoryPageQuery>(sender.Requests.Single());

            Assert.Equal(
                expected: 2,
                actual: query.Pagination.PageNumber);
            Assert.Equal(
                expected: 25,
                actual: query.Pagination.PageSize);
            Assert.Equal(
                expected: 11,
                actual: response.TotalCount);
            Assert.Equal(
                expected: 2,
                actual: response.PageNumber);
            Assert.Equal(
                expected: 25,
                actual: response.PageSize);
            Assert.Single(response.Items);
            Assert.False(
                response.Items.Single()
                   .IsActive);
            Assert.False(
                response.Items.Single()
                   .IsCurrent);
        }

        [Fact]
        public async Task RevokeEndpoints_ReturnNoContentAndSendCommands()
        {
            var sessionId = Guid.Parse("3c64f1c4-e2b2-4b99-a35c-577cd7f8c31c");
            var sender = new FakeSender();
            sender.Handle<RevokeMySessionCommand>(_ => { });
            sender.Handle<RevokeOtherMySessionsCommand>(_ => { });
            MySessionsController controller = AttachHttpContext(
                controller: new MySessionsController(sender),
                httpContext: CreateHttpContext(
                    path: "/api/me/sessions",
                    userId: Guid.Parse("35736ec3-d0ee-4fdc-89eb-b301599d7d11")));

            IActionResult revokeSessionResult = await controller.RevokeSession(
                sessionId: sessionId,
                cancellationToken: CancellationToken.None);
            IActionResult revokeOthersResult = await controller.RevokeOtherSessions(CancellationToken.None);

            Assert.IsType<NoContentResult>(revokeSessionResult);
            Assert.IsType<NoContentResult>(revokeOthersResult);
            Assert.Collection(
                collection: sender.Requests,
                request => Assert.Equal(
                    expected: sessionId,
                    actual: Assert.IsType<RevokeMySessionCommand>(request)
                       .SessionId),
                request => Assert.IsType<RevokeOtherMySessionsCommand>(request));
        }
    }
}
