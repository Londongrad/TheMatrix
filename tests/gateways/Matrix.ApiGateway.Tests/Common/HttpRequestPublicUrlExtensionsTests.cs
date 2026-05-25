using Matrix.ApiGateway.Common.Urls;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Matrix.ApiGateway.Tests.Common
{
    public sealed class HttpRequestPublicUrlExtensionsTests
    {
        [Fact]
        public void ToPublicUrl_WhenValueIsNullOrWhitespace_ReturnsNull()
        {
            DefaultHttpContext httpContext = CreateHttpContext();

            Assert.Null(httpContext.Request.ToPublicUrl(null));
            Assert.Null(httpContext.Request.ToPublicUrl(string.Empty));
            Assert.Null(httpContext.Request.ToPublicUrl("   "));
        }

        [Fact]
        public void ToPublicUrl_WhenValueIsAbsoluteUrl_ReturnsOriginalValue()
        {
            DefaultHttpContext httpContext = CreateHttpContext();

            string? result = httpContext.Request.ToPublicUrl("https://cdn.matrix.test/avatars/u-01.png");

            Assert.Equal(
                expected: "https://cdn.matrix.test/avatars/u-01.png",
                actual: result);
        }

        [Fact]
        public void ToPublicUrl_WhenValueIsRelativePath_BuildsAbsoluteGatewayUrlIncludingPathBase()
        {
            DefaultHttpContext httpContext = CreateHttpContext();
            httpContext.Request.PathBase = "/gateway";

            string? result = httpContext.Request.ToPublicUrl("avatars/u-01.png");

            Assert.Equal(
                expected: "https://gateway.test/gateway/avatars/u-01.png",
                actual: result);
        }

        [Fact]
        public void ToPublicUrl_WhenValueIsRootRelativePath_BuildsAbsoluteGatewayUrlIncludingPathBase()
        {
            DefaultHttpContext httpContext = CreateHttpContext();
            httpContext.Request.PathBase = "/gateway";

            string? result = httpContext.Request.ToPublicUrl("/avatars/u-01.png");

            Assert.Equal(
                expected: "https://gateway.test/gateway/avatars/u-01.png",
                actual: result);
        }

        private static DefaultHttpContext CreateHttpContext()
        {
            DefaultHttpContext httpContext = new();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("gateway.test");
            return httpContext;
        }
    }
}
