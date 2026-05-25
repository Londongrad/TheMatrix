using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Matrix.ApiGateway.Authorization.InternalJwt;
using Matrix.ApiGateway.Authorization.InternalJwt.Abstractions;
using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.HttpHandlers;
using Matrix.Identity.Contracts.Internal.Responses;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.ApiGateway.Tests.TestSupport.ApiGatewayTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Common
{
    public sealed class DownstreamAuthHandlersTests
    {
        [Fact]
        public async Task ForwardAuthorizationHeaderHandler_WhenAuthorizationHeaderExists_ForwardsIt()
        {
            DefaultHttpContext httpContext = new();
            httpContext.Request.Headers.Authorization = "Bearer external-token";
            var transport = new RecordingTerminalHandler();
            var handler = new ForwardAuthorizationHeaderHandler(
                new HttpContextAccessor
                {
                    HttpContext = httpContext
                })
            {
                InnerHandler = transport
            };
            using var invoker = new HttpMessageInvoker(handler);

            using HttpResponseMessage response = await invoker.SendAsync(
                request: new HttpRequestMessage(
                    method: HttpMethod.Get,
                    requestUri: "https://identity.test/api/me"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: HttpStatusCode.OK,
                actual: response.StatusCode);
            Assert.NotNull(transport.LastRequest);
            Assert.Equal(
                expected: "Bearer",
                actual: transport.LastRequest!.Headers.Authorization?.Scheme);
            Assert.Equal(
                expected: "external-token",
                actual: transport.LastRequest.Headers.Authorization?.Parameter);
        }

        [Fact]
        public async Task ForwardAuthorizationHeaderHandler_WhenHeaderIsMissing_DoesNotSetAuthorization()
        {
            DefaultHttpContext httpContext = new();
            var transport = new RecordingTerminalHandler();
            var handler = new ForwardAuthorizationHeaderHandler(
                new HttpContextAccessor
                {
                    HttpContext = httpContext
                })
            {
                InnerHandler = transport
            };
            using var invoker = new HttpMessageInvoker(handler);

            await invoker.SendAsync(
                request: new HttpRequestMessage(
                    method: HttpMethod.Get,
                    requestUri: "https://identity.test/api/me"),
                cancellationToken: CancellationToken.None);

            Assert.Null(transport.LastRequest!.Headers.Authorization);
        }

        [Fact]
        public async Task InternalJwtExchangeHandler_WhenAuthenticatedUserExists_IssuesInternalJwtFromStores()
        {
            var userId = Guid.Parse("20f3051a-b347-45de-a091-fb4d41e0f941");
            DefaultHttpContext httpContext = new();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    claims:
                    [
                        new Claim(
                            type: JwtRegisteredClaimNames.Sub,
                            value: userId.ToString()),
                        new Claim(
                            type: "jti",
                            value: "jwt-id-1")
                    ],
                    authenticationType: "gateway"));

            var permissionsStore = new FakePermissionsVersionStore
            {
                CurrentVersion = 11
            };
            var authContextStore = new FakeAuthContextStore();
            authContextStore.Responses[(userId, 11)] = new UserAuthContextResponse(
                PermissionsVersion: 11,
                EffectivePermissions:
                [
                    "cities.launch",
                    "population.read"
                ]);
            var issuer = new RecordingInternalJwtIssuer();
            var transport = new RecordingTerminalHandler();
            var handler = new InternalJwtExchangeHandler(
                http: new HttpContextAccessor
                {
                    HttpContext = httpContext
                },
                pvStore: permissionsStore,
                authContextStore: authContextStore,
                internalJwtIssuer: issuer,
                requestContextAccessor: new InternalJwtRequestContextAccessor())
            {
                InnerHandler = transport
            };
            using var invoker = new HttpMessageInvoker(handler);

            await invoker.SendAsync(
                request: new HttpRequestMessage(
                    method: HttpMethod.Get,
                    requestUri: "https://simulation.test/api/cities"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: userId,
                actual: permissionsStore.LastRequestedUserId);
            Assert.Equal(
                expected: (userId, 11),
                actual: authContextStore.LastRequest);
            Assert.Equal(
                expected: userId,
                actual: issuer.LastUserId);
            Assert.Equal(
                expected: "jwt-id-1",
                actual: issuer.LastJti);
            Assert.Equal(
                expected: 11,
                actual: issuer.LastPermissionsVersion);
            Assert.Equal(
                expected: "Bearer",
                actual: transport.LastRequest!.Headers.Authorization?.Scheme);
            Assert.Equal(
                expected: "internal-jwt",
                actual: transport.LastRequest.Headers.Authorization?.Parameter);
        }

        [Fact]
        public async Task InternalJwtExchangeHandler_WhenNoAuthenticatedUser_UsesRequestContextAccessorFallback()
        {
            var requestContextAccessor = new InternalJwtRequestContextAccessor();
            using IDisposable _ = requestContextAccessor.Push(
                new InternalJwtRequestContext(
                    UserId: Guid.Parse("a70d2f40-f46c-478f-b93c-c695ea29ba94"),
                    Jti: "queued-launch",
                    PermissionsVersion: 5,
                    EffectivePermissions: ["cities.launch"]));

            var issuer = new RecordingInternalJwtIssuer();
            var transport = new RecordingTerminalHandler();
            var handler = new InternalJwtExchangeHandler(
                http: new HttpContextAccessor
                {
                    HttpContext = new DefaultHttpContext()
                },
                pvStore: new FakePermissionsVersionStore(),
                authContextStore: new FakeAuthContextStore(),
                internalJwtIssuer: issuer,
                requestContextAccessor: requestContextAccessor)
            {
                InnerHandler = transport
            };
            using var invoker = new HttpMessageInvoker(handler);

            await invoker.SendAsync(
                request: new HttpRequestMessage(
                    method: HttpMethod.Get,
                    requestUri: "https://simulation.test/api/cities"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: Guid.Parse("a70d2f40-f46c-478f-b93c-c695ea29ba94"),
                actual: issuer.LastUserId);
            Assert.Equal(
                expected: "queued-launch",
                actual: issuer.LastJti);
            Assert.Equal(
                expected: 5,
                actual: issuer.LastPermissionsVersion);
            Assert.Equal(
                expected: "internal-jwt",
                actual: transport.LastRequest!.Headers.Authorization?.Parameter);
        }

        [Fact]
        public async Task InternalJwtExchangeHandler_WhenNoIdentityAndNoRequestContext_DoesNotSetAuthorization()
        {
            var transport = new RecordingTerminalHandler();
            var handler = new InternalJwtExchangeHandler(
                http: new HttpContextAccessor
                {
                    HttpContext = new DefaultHttpContext()
                },
                pvStore: new FakePermissionsVersionStore(),
                authContextStore: new FakeAuthContextStore(),
                internalJwtIssuer: new RecordingInternalJwtIssuer(),
                requestContextAccessor: new InternalJwtRequestContextAccessor())
            {
                InnerHandler = transport
            };
            using var invoker = new HttpMessageInvoker(handler);

            await invoker.SendAsync(
                request: new HttpRequestMessage(
                    method: HttpMethod.Get,
                    requestUri: "https://simulation.test/api/cities"),
                cancellationToken: CancellationToken.None);

            Assert.Null(transport.LastRequest!.Headers.Authorization);
        }

        private sealed class RecordingTerminalHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
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

            public string Issue(
                Guid userId,
                string? jti,
                int permissionsVersion,
                IReadOnlyCollection<string> permissions)
            {
                LastUserId = userId;
                LastJti = jti;
                LastPermissionsVersion = permissionsVersion;
                LastPermissions = permissions.ToArray();
                return "internal-jwt";
            }
        }
    }
}
