using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Matrix.ApiGateway.Authorization.Caching;
using Matrix.ApiGateway.Controllers.Identity.Self;
using Matrix.ApiGateway.Contracts.Identity.Requests;
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
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.Controllers.Identity;

public sealed class IdentitySelfControllersTests
{
    [Fact]
    public async Task AuthControllerLogin_SetsRefreshCookieAndStripsRefreshTokenFromPayload()
    {
        var authClient = new RecordingIdentityAuthClient
        {
            LoginResult = new LoginResponse
            {
                AccessToken = "access-token",
                ExpiresIn = 900,
                RefreshToken = "refresh-token",
                RefreshTokenExpiresAtUtc = new DateTime(2048, 6, 20, 12, 0, 0, DateTimeKind.Utc),
                IsPersistent = true
            }
        };
        var controller = new AuthController(authClient)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            }
        };

        ActionResult<LoginResponse> actionResult = await controller.Login(
            new LoginRequest
            {
                Login = "mira",
                Password = "Str0ng#Pass",
                DeviceId = "desktop",
                DeviceName = "Desktop",
                RememberMe = true
            },
            CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        LoginResponse payload = Assert.IsType<LoginResponse>(ok.Value);
        Assert.Equal("access-token", payload.AccessToken);
        Assert.Equal(string.Empty, payload.RefreshToken);
        string setCookie = controller.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("matrix_refresh_token=refresh-token", setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthControllerRefresh_WithoutCookie_ReturnsUnauthorizedProblem()
    {
        var controller = new AuthController(new RecordingIdentityAuthClient())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            }
        };

        ActionResult<LoginResponse> actionResult = await controller.Refresh(
            new RefreshRequestDto
            {
                DeviceId = "desktop"
            },
            CancellationToken.None);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        ProblemDetails problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        Assert.Equal("Auth.NoRefreshCookie", Assert.IsType<string>(problem.Extensions["code"]));
    }

    [Fact]
    public async Task AuthControllerRefresh_WhenDownstreamRejectsToken_ClearsCookieAndRethrows()
    {
        var authClient = new RecordingIdentityAuthClient
        {
            RefreshException = CreateDownstreamServiceException(
                statusCode: HttpStatusCode.Unauthorized,
                serviceName: "identity")
        };
        DefaultHttpContext httpContext = CreateHttpContext();
        httpContext.Request.Headers.Cookie = "matrix_refresh_token=refresh-token";
        var controller = new AuthController(authClient)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        await Assert.ThrowsAsync<Matrix.ApiGateway.DownstreamClients.Common.Exceptions.DownstreamServiceException>(
            () => controller.Refresh(
                new RefreshRequestDto
                {
                    DeviceId = "desktop"
                },
                CancellationToken.None));

        string setCookie = controller.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("matrix_refresh_token=", setCookie, StringComparison.Ordinal);
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthControllerLogout_WithCookie_DelegatesAndClearsCookie()
    {
        var authClient = new RecordingIdentityAuthClient();
        DefaultHttpContext httpContext = CreateHttpContext();
        httpContext.Request.Headers.Cookie = "matrix_refresh_token=refresh-token";
        var controller = new AuthController(authClient)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        IActionResult actionResult = await controller.Logout(CancellationToken.None);

        Assert.IsType<NoContentResult>(actionResult);
        Assert.Equal("refresh-token", authClient.LastLogoutRequest?.RefreshToken);
        Assert.Contains("matrix_refresh_token=", controller.HttpContext.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccountControllerGetProfile_NormalizesAvatarUrlToPublicAbsoluteUrl()
    {
        var accountClient = new RecordingIdentityAccountClient
        {
            ProfileResult = new UserProfileResponse
            {
                UserId = Guid.Parse("7a54363d-d4cc-4a87-b13b-a15d1d4d4a8c"),
                Email = "mira@matrix.test",
                Username = "mira",
                AvatarUrl = "/avatars/mira.png",
                IsEmailConfirmed = true,
                CreatedAtUtc = new DateTime(2048, 6, 1, 8, 0, 0, DateTimeKind.Utc),
                EffectivePermissions = ["cities.view"],
                PermissionsVersion = 5
            }
        };
        var controller = new AccountController(
            accountClient,
            new RecordingIdentityAssetsClient(),
            new RecordingDistributedCache())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            }
        };

        ActionResult<UserProfileResponse> actionResult = await controller.GetProfile(CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        UserProfileResponse profile = Assert.IsType<UserProfileResponse>(ok.Value);
        Assert.Equal("https://gateway.test/gw/avatars/mira.png", profile.AvatarUrl);
    }

    [Fact]
    public async Task AccountControllerChangeAvatar_WhenAvatarIsMissing_ReturnsBadRequestProblem()
    {
        var controller = new AccountController(
            new RecordingIdentityAccountClient(),
            new RecordingIdentityAssetsClient(),
            new RecordingDistributedCache())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            }
        };

        ActionResult<ChangeAvatarResponse> actionResult = await controller.ChangeAvatar(null, CancellationToken.None);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        ProblemDetails problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal("Gateway.EmptyAvatar", Assert.IsType<string>(problem.Extensions["code"]));
    }

    [Fact]
    public async Task AccountControllerDeleteAccount_WhenUserClaimExists_ClearsPermissionsVersionCache()
    {
        Guid userId = Guid.Parse("6996100f-3ad0-446d-8257-834a10ddf2ce");
        var cache = new RecordingDistributedCache();
        cache.SeedString(AuthorizationCacheKeys.PermissionsVersion(userId), "5");
        cache.SeedString(AuthorizationCacheKeys.PermissionsVersionStale(userId), "5");
        var accountClient = new RecordingIdentityAccountClient();
        var controller = new AccountController(
            accountClient,
            new RecordingIdentityAssetsClient(),
            cache)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(userId)
            }
        };

        IActionResult actionResult = await controller.DeleteAccount(
            new DeleteAccountRequest
            {
                CurrentPassword = "Str0ng#Pass"
            },
            CancellationToken.None);

        Assert.IsType<NoContentResult>(actionResult);
        Assert.NotNull(accountClient.LastDeleteAccountRequest);
        Assert.Null(cache.ReadString(AuthorizationCacheKeys.PermissionsVersion(userId)));
        Assert.Null(cache.ReadString(AuthorizationCacheKeys.PermissionsVersionStale(userId)));
    }

    [Fact]
    public async Task AccountControllerGetAvatar_WhenFileNameIsInvalid_ReturnsBadRequest()
    {
        var controller = new AccountController(
            new RecordingIdentityAccountClient(),
            new RecordingIdentityAssetsClient(),
            new RecordingDistributedCache())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            }
        };

        IActionResult actionResult = await controller.GetAvatar("../secret.png", CancellationToken.None);

        Assert.IsType<BadRequestResult>(actionResult);
    }

    [Fact]
    public async Task AccountControllerGetAvatar_WhenDownstreamSucceeds_ReturnsFileContent()
    {
        var assetsClient = new RecordingIdentityAssetsClient
        {
            AvatarResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([1, 2, 3, 4])
            }
        };
        assetsClient.AvatarResponse.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        var controller = new AccountController(
            new RecordingIdentityAccountClient(),
            assetsClient,
            new RecordingDistributedCache())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            }
        };

        IActionResult actionResult = await controller.GetAvatar("avatar.png", CancellationToken.None);

        FileContentResult file = Assert.IsType<FileContentResult>(actionResult);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal([1, 2, 3, 4], file.FileContents);
        Assert.Equal("avatar.png", assetsClient.LastFileName);
    }

    [Fact]
    public async Task SessionsControllerRevokeAllSessions_ClearsRefreshCookie()
    {
        var sessionsClient = new RecordingIdentitySessionsClient();
        var controller = new SessionsController(sessionsClient)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            }
        };

        IActionResult actionResult = await controller.RevokeAllSessions(CancellationToken.None);

        Assert.IsType<NoContentResult>(actionResult);
        Assert.Equal(1, sessionsClient.RevokeAllSessionsCallCount);
        Assert.Contains("matrix_refresh_token=", controller.HttpContext.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SessionsControllerGetSessionHistoryPage_ReturnsOkResult()
    {
        var sessionsClient = new RecordingIdentitySessionsClient
        {
            SessionHistoryResult = new PagedResult<SessionResponse>([], 0, 2, 25)
        };
        var controller = new SessionsController(sessionsClient)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            }
        };

        ActionResult<PagedResult<SessionResponse>> actionResult = await controller.GetSessionHistoryPage(2, 25, CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        PagedResult<SessionResponse> page = Assert.IsType<PagedResult<SessionResponse>>(ok.Value);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(2, sessionsClient.LastPageNumber);
        Assert.Equal(25, sessionsClient.LastPageSize);
    }

    private static DefaultHttpContext CreateHttpContext(Guid? userId = null)
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("gateway.test");
        httpContext.Request.PathBase = new PathString("/gw");

        if (userId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString())
            ], "gateway"));
        }

        return httpContext;
    }

    private sealed class RecordingIdentityAuthClient : IIdentityAuthClient
    {
        public LoginResponse? LoginResult { get; set; }
        public LoginResponse? RefreshResult { get; set; }
        public Exception? RefreshException { get; set; }
        public LogoutRequest? LastLogoutRequest { get; private set; }

        public Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new RegisterResponse { UserId = Guid.NewGuid(), Email = request.Email, Username = request.Username });

        public Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(LoginResult ?? new LoginResponse
            {
                AccessToken = "access",
                ExpiresIn = 900,
                RefreshToken = "refresh",
                RefreshTokenExpiresAtUtc = new DateTime(2048, 6, 20, 12, 0, 0, DateTimeKind.Utc)
            });

        public Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
        {
            if (RefreshException is not null)
                throw RefreshException;

            return Task.FromResult(RefreshResult ?? new LoginResponse
            {
                AccessToken = "refreshed-access",
                ExpiresIn = 900,
                RefreshToken = "new-refresh",
                RefreshTokenExpiresAtUtc = new DateTime(2048, 6, 21, 12, 0, 0, DateTimeKind.Utc)
            });
        }

        public Task SendEmailConfirmationAsync(SendEmailConfirmationRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RequestAccountRecoveryAsync(RequestAccountRecoveryRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ConfirmAccountRecoveryAsync(ConfirmAccountRecoveryRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
        {
            LastLogoutRequest = request;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingIdentityAccountClient : IIdentityAccountClient
    {
        public UserProfileResponse? ProfileResult { get; set; }
        public DeleteAccountRequest? LastDeleteAccountRequest { get; private set; }

        public Task<ChangeAvatarResponse> ChangeAvatarAsync(IFormFile avatar, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChangeAvatarResponse { AvatarUrl = "/avatars/changed.png" });

        public Task<ChangeDisplayNameResponse> ChangeDisplayNameAsync(ChangeDisplayNameRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChangeDisplayNameResponse { DisplayName = request.DisplayName });

        public Task<ChangeUsernameResponse> ChangeUsernameAsync(ChangeUsernameRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChangeUsernameResponse { Username = request.Username });

        public Task<ChangeEmailResponse> ChangeEmailAsync(ChangeEmailRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChangeEmailResponse { PendingEmail = request.NewEmail });

        public Task<ChangeAvatarResponse> ClearAvatarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ChangeAvatarResponse { AvatarUrl = null });

        public Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ResendPendingEmailChangeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CancelPendingEmailChangeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAccountAsync(DeleteAccountRequest request, CancellationToken cancellationToken = default)
        {
            LastDeleteAccountRequest = request;
            return Task.CompletedTask;
        }

        public Task<UserProfileResponse> GetProfileAsync(CancellationToken cancellationToken)
            => Task.FromResult(ProfileResult ?? new UserProfileResponse
            {
                UserId = Guid.NewGuid(),
                Email = "mira@matrix.test",
                Username = "mira",
                IsEmailConfirmed = true,
                CreatedAtUtc = new DateTime(2048, 6, 1, 8, 0, 0, DateTimeKind.Utc),
                EffectivePermissions = ["cities.view"],
                PermissionsVersion = 1
            });

        public Task<CursorPagedResult<SecurityActivityItemResponse>> GetSecurityActivityFeedAsync(string? cursor, int pageSize, CancellationToken cancellationToken)
            => Task.FromResult(new CursorPagedResult<SecurityActivityItemResponse>([], pageSize, null));
    }

    private sealed class RecordingIdentityAssetsClient : IIdentityAssetsClient
    {
        public HttpResponseMessage AvatarResponse { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([])
        };

        public string? LastFileName { get; private set; }

        public Task<HttpResponseMessage> GetAvatarAsync(string fileName, CancellationToken cancellationToken)
        {
            LastFileName = fileName;
            return Task.FromResult(AvatarResponse);
        }
    }

    private sealed class RecordingIdentitySessionsClient : IIdentitySessionsClient
    {
        public PagedResult<SessionResponse>? SessionHistoryResult { get; set; }
        public int RevokeAllSessionsCallCount { get; private set; }
        public int? LastPageNumber { get; private set; }
        public int? LastPageSize { get; private set; }

        public Task<IReadOnlyCollection<SessionResponse>> GetSessionsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<SessionResponse>>([]);

        public Task<PagedResult<SessionResponse>> GetSessionHistoryPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            LastPageNumber = pageNumber;
            LastPageSize = pageSize;
            return Task.FromResult(SessionHistoryResult ?? new PagedResult<SessionResponse>([], 0, pageNumber, pageSize));
        }

        public Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RevokeOtherSessionsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RevokeAllSessionsAsync(CancellationToken cancellationToken = default)
        {
            RevokeAllSessionsCallCount++;
            return Task.CompletedTask;
        }
    }
}
