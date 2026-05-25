using Matrix.Identity.Api.Authorization.Internal;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.IdentityApiTestSupport;

namespace Matrix.Identity.Api.Tests.Authorization.Internal
{
    public sealed class TrustedGatewayClientIpResolverTests
    {
        [Fact]
        public void Resolve_WhenTrustedGatewayProvidesForwardedClientIp_ReturnsNormalizedForwardedIp()
        {
            DefaultHttpContext context = CreateHttpContext(
                remoteIp: "198.51.100.10",
                forwardedClientIp: "::ffff:203.0.113.77",
                trustedGateway: true);

            string? result = TrustedGatewayClientIpResolver.Resolve(context);

            Assert.Equal(
                expected: "203.0.113.77",
                actual: result);
        }

        [Fact]
        public void Resolve_WhenTrustedGatewayHeaderIsInvalid_FallsBackToRemoteIp()
        {
            DefaultHttpContext context = CreateHttpContext(
                remoteIp: "::ffff:198.51.100.10",
                forwardedClientIp: "not-an-ip",
                trustedGateway: true);

            string? result = TrustedGatewayClientIpResolver.Resolve(context);

            Assert.Equal(
                expected: "198.51.100.10",
                actual: result);
        }

        [Fact]
        public void Resolve_WhenRequestIsNotTrusted_IgnoresForwardedHeader()
        {
            DefaultHttpContext context = CreateHttpContext(
                remoteIp: "198.51.100.10",
                forwardedClientIp: "203.0.113.77",
                trustedGateway: false);

            string? result = TrustedGatewayClientIpResolver.Resolve(context);

            Assert.Equal(
                expected: "198.51.100.10",
                actual: result);
        }
    }
}
