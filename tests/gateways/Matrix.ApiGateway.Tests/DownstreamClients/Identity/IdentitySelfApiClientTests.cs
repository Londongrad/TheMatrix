using System.Net;
using System.Text;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Account;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Assets;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Auth;
using Matrix.ApiGateway.DownstreamClients.Identity.Self.Sessions;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Identity.Contracts.Self.Account.Requests;
using Matrix.Identity.Contracts.Self.Account.Responses;
using Matrix.Identity.Contracts.Self.Auth.Requests;
using Matrix.Identity.Contracts.Self.Auth.Responses;
using Matrix.Identity.Contracts.Self.Sessions.Responses;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Identity;

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
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, response))
        };
        IIdentityAuthClient client = CreateIdentityAuthApiClient(CreateHttpClient(handler));

        RegisterResponse result = await client.RegisterAsync(
            new RegisterRequest
            {
                Email = "mira@matrix.test",
                Username = "mira",
                Password = "Str0ng#Pass"
            },
            CancellationToken.None);

        Assert.Equal(response.UserId, result.UserId);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/api/auth/register", request.RequestUri, StringComparison.Ordinal);
        Assert.Equal("application/json", request.ContentType);
        Assert.Contains("\"email\":\"mira@matrix.test\"", request.Body, StringComparison.Ordinal);
        Assert.Contains("\"username\":\"mira\"", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityAuthApiClientRefreshAsync_WhenCalled_PostsRefreshPayload()
    {
        LoginResponse response = new()
        {
            AccessToken = "jwt-access",
            ExpiresIn = 900,
            RefreshToken = "refresh-002",
            RefreshTokenExpiresAtUtc = new DateTime(2048, 6, 10, 12, 0, 0, DateTimeKind.Utc),
            IsPersistent = true
        };
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, response))
        };
        IIdentityAuthClient client = CreateIdentityAuthApiClient(CreateHttpClient(handler));

        LoginResponse result = await client.RefreshAsync(
            new RefreshRequest
            {
                RefreshToken = "refresh-002",
                DeviceId = "device-chita"
            },
            CancellationToken.None);

        Assert.Equal("jwt-access", result.AccessToken);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith("/api/auth/refresh", request.RequestUri, StringComparison.Ordinal);
        Assert.Contains("\"deviceId\":\"device-chita\"", request.Body, StringComparison.Ordinal);
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
                    CreatedAtUtc = new DateTime(2048, 6, 11, 9, 0, 0, DateTimeKind.Utc),
                    LastUsedAtUtc = new DateTime(2048, 6, 11, 9, 30, 0, DateTimeKind.Utc),
                    RefreshTokenExpiresAtUtc = new DateTime(2048, 7, 11, 9, 0, 0, DateTimeKind.Utc),
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
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, page))
        };
        IIdentitySessionsClient client = CreateIdentitySessionsApiClient(CreateHttpClient(handler));

        PagedResult<SessionResponse> result = await client.GetSessionHistoryPageAsync(2, 25, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith("/api/me/sessions/history?pageNumber=2&pageSize=25", request.RequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentitySessionsApiClientRevokeOtherSessionsAsync_WhenDownstreamFails_ThrowsDownstreamServiceException()
    {
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateStringResponse(HttpStatusCode.BadGateway, "{\"error\":\"revoke-failed\"}"))
        };
        IIdentitySessionsClient client = CreateIdentitySessionsApiClient(CreateHttpClient(handler));

        DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(
            () => client.RevokeOtherSessionsAsync(CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Contains("revoke-failed", exception.Body, StringComparison.Ordinal);
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
                    OccurredAtUtc = new DateTime(2048, 6, 12, 15, 0, 0, DateTimeKind.Utc),
                    IpAddress = "203.0.113.25",
                    DeviceName = "Desktop"
                }
            ],
            pageSize: 20,
            nextCursor: "cursor-2");
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, page))
        };
        IIdentityAccountClient client = CreateIdentityAccountApiClient(CreateHttpClient(handler));

        CursorPagedResult<SecurityActivityItemResponse> result = await client.GetSecurityActivityFeedAsync(
            cursor: "cursor-1",
            pageSize: 20,
            cancellationToken: CancellationToken.None);

        Assert.Equal("cursor-2", result.NextCursor);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.EndsWith("/api/account/security-activity?pageSize=20&cursor=cursor-1", request.RequestUri, StringComparison.Ordinal);
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
            OnSendAsync = (_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, response))
        };
        IIdentityAccountClient client = CreateIdentityAccountApiClient(CreateHttpClient(handler));
        byte[] content = Encoding.UTF8.GetBytes("avatar-bytes");
        var avatar = new FormFile(new MemoryStream(content), 0, content.Length, "avatar", "avatar.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        ChangeAvatarResponse result = await client.ChangeAvatarAsync(avatar, CancellationToken.None);

        Assert.Equal("/avatars/mira.png", result.AvatarUrl);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Put, request.Method);
        Assert.EndsWith("/api/account/avatar", request.RequestUri, StringComparison.Ordinal);
        Assert.Equal("multipart/form-data", request.ContentType);
        Assert.Contains("avatar.png", request.Body, StringComparison.Ordinal);
        Assert.Contains("avatar-bytes", request.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IdentityAssetsApiClientGetAvatarAsync_WhenCalled_UsesAvatarPath()
    {
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateStringResponse(HttpStatusCode.OK, "image-bytes", "image/png"))
        };
        IIdentityAssetsClient client = CreateIdentityAssetsApiClient(CreateHttpClient(handler));

        using HttpResponseMessage result = await client.GetAvatarAsync("mira.png", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.EndsWith("/avatars/mira.png", request.RequestUri, StringComparison.Ordinal);
    }
}
