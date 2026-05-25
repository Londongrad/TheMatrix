using Matrix.Identity.Api.Authorization.Internal;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.Identity.Api.Tests.TestSupport.IdentityApiTestSupport;

namespace Matrix.Identity.Api.Tests.Authorization.Internal
{
    public sealed class InternalApiKeyMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WhenInternalPathHasNoKey_ReturnsUnauthorized()
        {
            bool nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            DefaultHttpContext context = CreateHttpContext(path: "/api/internal/users");
            var middleware = new InternalApiKeyMiddleware(
                next: next,
                options: CreateInternalOptions());

            await middleware.InvokeAsync(context);

            Assert.Equal(
                expected: StatusCodes.Status401Unauthorized,
                actual: context.Response.StatusCode);
            Assert.False(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_WhenInternalPathHasInvalidKey_ReturnsForbidden()
        {
            bool nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            DefaultHttpContext context = CreateHttpContext(path: "/api/internal/authorization");
            context.Request.Headers[InternalApiKeyMiddleware.ApiKeyHeaderName] = "wrong-key";
            var middleware = new InternalApiKeyMiddleware(
                next: next,
                options: CreateInternalOptions());

            await middleware.InvokeAsync(context);

            Assert.Equal(
                expected: StatusCodes.Status403Forbidden,
                actual: context.Response.StatusCode);
            Assert.False(nextCalled);
        }

        [Fact]
        public async Task InvokeAsync_WhenInternalPathHasValidNamedKey_MarksTrustedAndCallsNext()
        {
            bool nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            DefaultHttpContext context = CreateHttpContext(path: "/api/internal/users");
            context.Request.Headers[InternalApiKeyMiddleware.ApiKeyIdHeaderName] = "current";
            context.Request.Headers[InternalApiKeyMiddleware.ApiKeyHeaderName] = "Z9y8X7w6V5u4T3s2R1q0P)o(I*u&Y^t%R$";
            var middleware = new InternalApiKeyMiddleware(
                next: next,
                options: CreateInternalOptions(
                    apiKey: string.Empty,
                    currentKeyId: "current",
                    keys: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["current"] = "Z9y8X7w6V5u4T3s2R1q0P)o(I*u&Y^t%R$"
                    }));

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.True(TrustedGatewayRequestContext.IsTrusted(context));
            Assert.Equal(
                expected: StatusCodes.Status200OK,
                actual: context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WhenPublicPathHasNoKey_PassesThrough()
        {
            bool nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            DefaultHttpContext context = CreateHttpContext(path: "/api/auth/login");
            var middleware = new InternalApiKeyMiddleware(
                next: next,
                options: CreateInternalOptions());

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.False(TrustedGatewayRequestContext.IsTrusted(context));
            Assert.Equal(
                expected: StatusCodes.Status200OK,
                actual: context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WhenPublicPathHasValidKey_MarksTrustedAndCallsNext()
        {
            bool nextCalled = false;
            RequestDelegate next = _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };
            DefaultHttpContext context = CreateHttpContext(path: "/api/misplaced-internal");
            context.Request.Headers[InternalApiKeyMiddleware.ApiKeyHeaderName] =
                "A1b2C3d4E5f6G7h8I9j0K!m@N#p$Q%r^S&";
            var middleware = new InternalApiKeyMiddleware(
                next: next,
                options: CreateInternalOptions());

            await middleware.InvokeAsync(context);

            Assert.True(nextCalled);
            Assert.True(TrustedGatewayRequestContext.IsTrusted(context));
            Assert.Equal(
                expected: StatusCodes.Status200OK,
                actual: context.Response.StatusCode);
        }
    }
}
