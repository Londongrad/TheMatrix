using System.Net;
using System.Text;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Assets;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Auth;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Sessions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Self.Account.Responses;
using Matrix.Identity.Contracts.Self.Auth.Requests;
using Matrix.Identity.Contracts.Self.Auth.Responses;
using Matrix.Identity.Contracts.Self.Sessions.Responses;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Identity
{
    public sealed class IdentitySelfApiClientTests
    {
        [Fact]
        public async Task IdentityAuthApiClientRegisterAsync_WhenCalled_PostsJsonAndReturnsPayload()
        {
            RegisterResponse response = new()
            {
                UserId = Guid.Parse("d1ecdd47-e34f-4c3a-b561-8c215635a01f"),
                Email = "mira@matrix.test",
                Username = "mira"
            };
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: response))
            };
            IIdentityAuthClient client = CreateIdentityAuthApiClient(CreateHttpClient(handler));

            RegisterResponse result = await client.RegisterAsync(
                request: new RegisterRequest
                {
                    Email = "mira@matrix.test",
                    Username = "mira",
                    Password = "Str0ng#Pass"
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: response.UserId,
                actual: result.UserId);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Post,
                actual: request.Method);
            Assert.EndsWith(
                expectedEndString: "/api/auth/register",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
            Assert.Equal(
                expected: "application/json",
                actual: request.ContentType);
            Assert.Contains(
                expectedSubstring: "\"email\":\"mira@matrix.test\"",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"username\":\"mira\"",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task IdentityAuthApiClientRefreshAsync_WhenCalled_PostsRefreshPayload()
        {
            LoginResponse response = new()
            {
                AccessToken = "jwt-access",
                ExpiresIn = 900,
                RefreshToken = "refresh-002",
                RefreshTokenExpiresAtUtc = new DateTime(
                    year: 2048,
                    month: 6,
                    day: 10,
                    hour: 12,
                    minute: 0,
                    second: 0,
                    kind: DateTimeKind.Utc),
                IsPersistent = true
            };
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: response))
            };
            IIdentityAuthClient client = CreateIdentityAuthApiClient(CreateHttpClient(handler));

            LoginResponse result = await client.RefreshAsync(
                request: new RefreshRequest
                {
                    RefreshToken = "refresh-002",
                    DeviceId = "device-chita"
                },
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "jwt-access",
                actual: result.AccessToken);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: "/api/auth/refresh",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"deviceId\":\"device-chita\"",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task IdentitySessionsApiClientGetSessionHistoryPageAsync_WhenCalled_UsesPaginationQuery()
        {
            PagedResult<SessionResponse> page = new(
                items:
                [
                    new SessionResponse
                    {
                        Id = Guid.Parse("48319c53-6714-4932-8d3f-a2f996f4fcff"),
                        DeviceId = "device-main",
                        DeviceName = "Desktop",
                        UserAgent = "Mozilla/5.0",
                        IpAddress = "203.0.113.5",
                        Country = "RU",
                        Region = "Zabaykalsky Krai",
                        City = "Chita",
                        CreatedAtUtc = new DateTime(
                            year: 2048,
                            month: 6,
                            day: 11,
                            hour: 9,
                            minute: 0,
                            second: 0,
                            kind: DateTimeKind.Utc),
                        LastUsedAtUtc = new DateTime(
                            year: 2048,
                            month: 6,
                            day: 11,
                            hour: 9,
                            minute: 30,
                            second: 0,
                            kind: DateTimeKind.Utc),
                        RefreshTokenExpiresAtUtc = new DateTime(
                            year: 2048,
                            month: 7,
                            day: 11,
                            hour: 9,
                            minute: 0,
                            second: 0,
                            kind: DateTimeKind.Utc),
                        IsActive = false,
                        IsCurrent = false,
                        IsPersistent = true
                    }
                ],
                totalCount: 1,
                pageNumber: 2,
                pageSize: 25);
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: page))
            };
            IIdentitySessionsClient client = CreateIdentitySessionsApiClient(CreateHttpClient(handler));

            PagedResult<SessionResponse> result = await client.GetSessionHistoryPageAsync(
                pageNumber: 2,
                pageSize: 25,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 1,
                actual: result.TotalCount);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: "/api/me/sessions/history?pageNumber=2&pageSize=25",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task
            IdentitySessionsApiClientRevokeOtherSessionsAsync_WhenDownstreamFails_ThrowsDownstreamServiceException()
        {
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateStringResponse(
                        statusCode: HttpStatusCode.BadGateway,
                        payload: "{\"error\":\"revoke-failed\"}"))
            };
            IIdentitySessionsClient client = CreateIdentitySessionsApiClient(CreateHttpClient(handler));

            DownstreamServiceException exception =
                await Assert.ThrowsAsync<DownstreamServiceException>(()
                    => client.RevokeOtherSessionsAsync(CancellationToken.None));

            Assert.Equal(
                expected: HttpStatusCode.BadGateway,
                actual: exception.StatusCode);
            Assert.Contains(
                expectedSubstring: "revoke-failed",
                actualString: exception.Body,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task IdentityAccountApiClientGetSecurityActivityFeedAsync_WhenCalled_UsesCursorQuery()
        {
            CursorPagedResult<SecurityActivityItemResponse> page = new(
                items:
                [
                    new SecurityActivityItemResponse
                    {
                        EventId = Guid.Parse("0fcb96da-a29c-45e4-9ca5-8c73a4da5f48"),
                        EventType = "PasswordChanged",
                        IsSuccessful = true,
                        OccurredAtUtc = new DateTime(
                            year: 2048,
                            month: 6,
                            day: 12,
                            hour: 15,
                            minute: 0,
                            second: 0,
                            kind: DateTimeKind.Utc),
                        IpAddress = "203.0.113.25",
                        DeviceName = "Desktop"
                    }
                ],
                pageSize: 20,
                nextCursor: "cursor-2");
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: page))
            };
            IIdentityAccountClient client = CreateIdentityAccountApiClient(CreateHttpClient(handler));

            CursorPagedResult<SecurityActivityItemResponse> result = await client.GetSecurityActivityFeedAsync(
                cursor: "cursor-1",
                pageSize: 20,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "cursor-2",
                actual: result.NextCursor);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.EndsWith(
                expectedEndString: "/api/account/security-activity?pageSize=20&cursor=cursor-1",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task IdentityAccountApiClientChangeAvatarAsync_WhenCalled_UsesMultipartUpload()
        {
            ChangeAvatarResponse response = new()
            {
                AvatarUrl = "/avatars/mira.png"
            };
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateJsonResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: response))
            };
            IIdentityAccountClient client = CreateIdentityAccountApiClient(CreateHttpClient(handler));
            byte[] content = Encoding.UTF8.GetBytes("avatar-bytes");
            var avatar = new FormFile(
                baseStream: new MemoryStream(content),
                baseStreamOffset: 0,
                length: content.Length,
                name: "avatar",
                fileName: "avatar.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            ChangeAvatarResponse result = await client.ChangeAvatarAsync(
                avatar: avatar,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "/avatars/mira.png",
                actual: result.AvatarUrl);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Put,
                actual: request.Method);
            Assert.EndsWith(
                expectedEndString: "/api/account/avatar",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
            Assert.Equal(
                expected: "multipart/form-data",
                actual: request.ContentType);
            Assert.Contains(
                expectedSubstring: "avatar.png",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "avatar-bytes",
                actualString: request.Body,
                comparisonType: StringComparison.Ordinal);
        }

        [Fact]
        public async Task IdentityAssetsApiClientGetAvatarAsync_WhenCalled_UsesAvatarPath()
        {
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(
                    CreateStringResponse(
                        statusCode: HttpStatusCode.OK,
                        payload: "image-bytes",
                        contentType: "image/png"))
            };
            IIdentityAssetsClient client = CreateIdentityAssetsApiClient(CreateHttpClient(handler));

            using HttpResponseMessage result = await client.GetAvatarAsync(
                fileName: "mira.png",
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: HttpStatusCode.OK,
                actual: result.StatusCode);
            RecordedRequest request = Assert.Single(handler.Requests);
            Assert.Equal(
                expected: HttpMethod.Get,
                actual: request.Method);
            Assert.EndsWith(
                expectedEndString: "/avatars/mira.png",
                actualString: request.RequestUri,
                comparisonType: StringComparison.Ordinal);
        }
    }
}
