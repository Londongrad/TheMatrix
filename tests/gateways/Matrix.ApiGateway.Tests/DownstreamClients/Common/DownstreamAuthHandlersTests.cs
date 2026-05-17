using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.Authorization.InternalJwt.Abstractions;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.HttpHandlers;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Common;

public sealed class DownstreamAuthHandlersTests
{
    [Fact]
    public async Task ForwardAuthorizationHeaderHandler_WhenAuthorizationHeaderExists_ForwardsIt()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers.Authorization = "Bearer external-token";
        var transport = new RecordingTerminalHandler();
        var handler = new ForwardAuthorizationHeaderHandler(new HttpContextAccessor { HttpContext = httpContext })
        {
            InnerHandler = transport
        };
        using var invoker = new HttpMessageInvoker(handler);

        using HttpResponseMessage response = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://identity.test/api/me"),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(transport.LastRequest);
        Assert.Equal("Bearer", transport.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("external-token", transport.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task ForwardAuthorizationHeaderHandler_WhenHeaderIsMissing_DoesNotSetAuthorization()
    {
        DefaultHttpContext httpContext = new();
        var transport = new RecordingTerminalHandler();
        var handler = new ForwardAuthorizationHeaderHandler(new HttpContextAccessor { HttpContext = httpContext })
        {
            InnerHandler = transport
        };
        using var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://identity.test/api/me"), CancellationToken.None);

        Assert.Null(transport.LastRequest!.Headers.Authorization);
    }

    [Fact]
    public async Task InternalJwtExchangeHandler_WhenAuthenticatedUserExists_IssuesInternalJwtFromStores()
    {
        Guid userId = Guid.Parse("20f3051a-b347-45de-a091-fb4d41e0f941");
        DefaultHttpContext httpContext = new();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim("jti", "jwt-id-1")
        ], "gateway"));

        var permissionsStore = new FakePermissionsVersionStore
        {
            CurrentVersion = 11
        };
        var authContextStore = new FakeAuthContextStore();
        authContextStore.Responses[(userId, 11)] = new UserAuthContextResponse(
            PermissionsVersion: 11,
            EffectivePermissions: ["cities.launch", "population.read"]);
        var issuer = new RecordingInternalJwtIssuer();
        var transport = new RecordingTerminalHandler();
        var handler = new InternalJwtExchangeHandler(
            new HttpContextAccessor { HttpContext = httpContext },
            permissionsStore,
            authContextStore,
            issuer,
            new InternalJwtRequestContextAccessor())
        {
            InnerHandler = transport
        };
        using var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://simulation.test/api/cities"), CancellationToken.None);

        Assert.Equal(userId, permissionsStore.LastRequestedUserId);
        Assert.Equal((userId, 11), authContextStore.LastRequest);
        Assert.Equal(userId, issuer.LastUserId);
        Assert.Equal("jwt-id-1", issuer.LastJti);
        Assert.Equal(11, issuer.LastPermissionsVersion);
        Assert.Equal("Bearer", transport.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal("internal-jwt", transport.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task InternalJwtExchangeHandler_WhenNoAuthenticatedUser_UsesRequestContextAccessorFallback()
    {
        var requestContextAccessor = new InternalJwtRequestContextAccessor();
        using IDisposable _ = requestContextAccessor.Push(new InternalJwtRequestContext(
            UserId: Guid.Parse("a70d2f40-f46c-478f-b93c-c695ea29ba94"),
            Jti: "queued-launch",
            PermissionsVersion: 5,
            EffectivePermissions: ["cities.launch"]));

        var issuer = new RecordingInternalJwtIssuer();
        var transport = new RecordingTerminalHandler();
        var handler = new InternalJwtExchangeHandler(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            new FakePermissionsVersionStore(),
            new FakeAuthContextStore(),
            issuer,
            requestContextAccessor)
        {
            InnerHandler = transport
        };
        using var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://simulation.test/api/cities"), CancellationToken.None);

        Assert.Equal(Guid.Parse("a70d2f40-f46c-478f-b93c-c695ea29ba94"), issuer.LastUserId);
        Assert.Equal("queued-launch", issuer.LastJti);
        Assert.Equal(5, issuer.LastPermissionsVersion);
        Assert.Equal("internal-jwt", transport.LastRequest!.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task InternalJwtExchangeHandler_WhenNoIdentityAndNoRequestContext_DoesNotSetAuthorization()
    {
        var transport = new RecordingTerminalHandler();
        var handler = new InternalJwtExchangeHandler(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            new FakePermissionsVersionStore(),
            new FakeAuthContextStore(),
            new RecordingInternalJwtIssuer(),
            new InternalJwtRequestContextAccessor())
        {
            InnerHandler = transport
        };
        using var invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://simulation.test/api/cities"), CancellationToken.None);

        Assert.Null(transport.LastRequest!.Headers.Authorization);
    }

    private sealed class RecordingTerminalHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class RecordingInternalJwtIssuer : IInternalJwtIssuer
    {
        public Guid LastUserId { get; private set; }
        public string? LastJti { get; private set; }
        public int LastPermissionsVersion { get; private set; }
        public IReadOnlyCollection<string>? LastPermissions { get; private set; }

        public string Issue(Guid userId, string? jti, int permissionsVersion, IReadOnlyCollection<string> permissions)
        {
            LastUserId = userId;
            LastJti = jti;
            LastPermissionsVersion = permissionsVersion;
            LastPermissions = permissions.ToArray();
            return "internal-jwt";
        }
    }
}
