using System.Net;
using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.ApiGateway.DownstreamClients.Identity;
using Matrix.BuildingBlocks.Api.Forwarding;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Identity
{
    public sealed class IdentityDelegatingHandlersTests
    {
        [Fact]
        public async Task InternalIdentityApiKeyAuthenticationHandler_WhenKeyRingConfigured_SetsCurrentKeyHeaders()
        {
            var transport = new RecordingTerminalHandler();
            var handler = new InternalIdentityApiKeyAuthenticationHandler(
                Options.Create(
                    new IdentityInternalOptions
                    {
                        BaseUrl = "https://identity.test",
                        RequestTimeoutSeconds = 10,
                        CurrentKeyId = "kid-02",
                        Keys = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["kid-01"] = "0123456789abcdef0123456789abcdef",
                            ["kid-02"] = "abcdef0123456789abcdef0123456789"
                        }
                    }))
            {
                InnerHandler = transport
            };
            using var invoker = new HttpMessageInvoker(handler);
            using var request = new HttpRequestMessage(
                method: HttpMethod.Get,
                requestUri: "https://identity.test/internal/users");
            request.Headers.Add(
                name: InternalIdentityApiKeyAuthenticationHandler.InternalApiKeyHeaderName,
                value: "stale-value");
            request.Headers.Add(
                name: InternalIdentityApiKeyAuthenticationHandler.InternalApiKeyIdHeaderName,
                value: "stale-id");

            await invoker.SendAsync(
                request: request,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "kid-02",
                actual: transport.LastRequest!.Headers
                   .GetValues(InternalIdentityApiKeyAuthenticationHandler.InternalApiKeyIdHeaderName)
                   .Single());
            Assert.Equal(
                expected: "abcdef0123456789abcdef0123456789",
                actual: transport.LastRequest.Headers
                   .GetValues(InternalIdentityApiKeyAuthenticationHandler.InternalApiKeyHeaderName)
                   .Single());
        }

        [Fact]
        public async Task InternalIdentityApiKeyAuthenticationHandler_WhenLegacyApiKeyConfigured_UsesLegacyKeyId()
        {
            var transport = new RecordingTerminalHandler();
            var handler = new InternalIdentityApiKeyAuthenticationHandler(
                Options.Create(
                    new IdentityInternalOptions
                    {
                        BaseUrl = "https://identity.test",
                        RequestTimeoutSeconds = 10,
                        ApiKey = "0123456789abcdef0123456789abcdef"
                    }))
            {
                InnerHandler = transport
            };
            using var invoker = new HttpMessageInvoker(handler);

            await invoker.SendAsync(
                request: new HttpRequestMessage(
                    method: HttpMethod.Get,
                    requestUri: "https://identity.test/internal/users"),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "legacy",
                actual: transport.LastRequest!.Headers
                   .GetValues(InternalIdentityApiKeyAuthenticationHandler.InternalApiKeyIdHeaderName)
                   .Single());
            Assert.Equal(
                expected: "0123456789abcdef0123456789abcdef",
                actual: transport.LastRequest.Headers
                   .GetValues(InternalIdentityApiKeyAuthenticationHandler.InternalApiKeyHeaderName)
                   .Single());
        }

        [Fact]
        public async Task
            TrustedIdentityClientContextHandler_WhenContextHasClientIpAndUserAgent_ForwardsNormalizedHeaders()
        {
            DefaultHttpContext httpContext = new();
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:10.10.1.25");
            httpContext.Request.Headers.UserAgent = "matrix-tests/1.0";
            var transport = new RecordingTerminalHandler();
            var handler = new TrustedIdentityClientContextHandler(
                new HttpContextAccessor
                {
                    HttpContext = httpContext
                })
            {
                InnerHandler = transport
            };
            using var invoker = new HttpMessageInvoker(handler);
            using var request = new HttpRequestMessage(
                method: HttpMethod.Get,
                requestUri: "https://identity.test/self/account");
            request.Headers.Add(
                name: TrustedGatewayClientHeaders.ClientIpHeaderName,
                value: "stale");
            request.Headers.UserAgent.ParseAdd("stale-agent");

            await invoker.SendAsync(
                request: request,
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "10.10.1.25",
                actual: transport.LastRequest!.Headers.GetValues(TrustedGatewayClientHeaders.ClientIpHeaderName)
                   .Single());
            Assert.Equal(
                expected: "matrix-tests/1.0",
                actual: transport.LastRequest.Headers.UserAgent.ToString());
        }

        [Fact]
        public async Task TrustedIdentityClientContextHandler_WhenContextIsMissing_DoesNotAddHeaders()
        {
            var transport = new RecordingTerminalHandler();
            var handler = new TrustedIdentityClientContextHandler(new HttpContextAccessor())
            {
                InnerHandler = transport
            };
            using var invoker = new HttpMessageInvoker(handler);

            await invoker.SendAsync(
                request: new HttpRequestMessage(
                    method: HttpMethod.Get,
                    requestUri: "https://identity.test/self/account"),
                cancellationToken: CancellationToken.None);

            Assert.False(transport.LastRequest!.Headers.Contains(TrustedGatewayClientHeaders.ClientIpHeaderName));
            Assert.Equal(
                expected: string.Empty,
                actual: transport.LastRequest.Headers.UserAgent.ToString());
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
    }
}
