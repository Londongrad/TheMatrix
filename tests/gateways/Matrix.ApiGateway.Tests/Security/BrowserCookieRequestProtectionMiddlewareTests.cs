using System.Text.Json;
using Matrix.ApiGateway.Configurations.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Matrix.ApiGateway.Tests.Security;

public sealed class BrowserCookieRequestProtectionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenProtectionIsDisabled_CallsNext()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(
            next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new FrontendSecurityOptions
            {
                EnforceCookieOriginProtection = false
            });
        DefaultHttpContext httpContext = CreateHttpContext(
            method: HttpMethods.Post,
            path: "/api/auth/refresh");

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WhenSecFetchSiteIsCrossSite_ReturnsForbiddenProblem()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        DefaultHttpContext httpContext = CreateHttpContext(
            method: HttpMethods.Post,
            path: "/api/auth/refresh");
        httpContext.Request.Headers["Sec-Fetch-Site"] = "cross-site";

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        JsonDocument payload = await ReadJsonAsync(httpContext);
        Assert.Equal("Gateway.CrossSiteCookieRequestRejected", payload.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenOriginIsAllowed_CallsNext()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(
            next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new FrontendSecurityOptions
            {
                EnforceCookieOriginProtection = true,
                AllowedOrigins = ["https://frontend.matrix.test"]
            });
        DefaultHttpContext httpContext = CreateHttpContext(
            method: HttpMethods.Post,
            path: "/api/auth/logout");
        httpContext.Request.Headers.Origin = "https://frontend.matrix.test";

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenRefererMatchesHostOrigin_CallsNext()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(
            next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
        DefaultHttpContext httpContext = CreateHttpContext(
            method: HttpMethods.Post,
            path: "/api/auth/refresh");
        httpContext.Request.Headers.Referer = "https://gateway.test/some-page";

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenOriginIsUntrusted_ReturnsForbiddenProblem()
    {
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        DefaultHttpContext httpContext = CreateHttpContext(
            method: HttpMethods.Post,
            path: "/api/auth/logout");
        httpContext.Request.Headers.Origin = "https://evil.test";

        await middleware.InvokeAsync(httpContext);

        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
        JsonDocument payload = await ReadJsonAsync(httpContext);
        Assert.Equal("Gateway.UntrustedCookieRequestOrigin", payload.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task InvokeAsync_WhenOriginCannotBeResolved_CallsNext()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(
            next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
        DefaultHttpContext httpContext = CreateHttpContext(
            method: HttpMethods.Post,
            path: "/api/auth/refresh");

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenPathIsNotProtected_CallsNext()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(
            next: _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });
        DefaultHttpContext httpContext = CreateHttpContext(
            method: HttpMethods.Post,
            path: "/api/auth/login");
        httpContext.Request.Headers.Origin = "https://evil.test";

        await middleware.InvokeAsync(httpContext);

        Assert.True(nextCalled);
    }

    private static BrowserCookieRequestProtectionMiddleware CreateMiddleware(
        RequestDelegate next,
        FrontendSecurityOptions? options = null)
    {
        return new BrowserCookieRequestProtectionMiddleware(
            next,
            Options.Create(options ?? new FrontendSecurityOptions
            {
                EnforceCookieOriginProtection = true
            }));
    }

    private static DefaultHttpContext CreateHttpContext(string method, string path)
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Method = method;
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("gateway.test");
        httpContext.Request.Path = path;
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<JsonDocument> ReadJsonAsync(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(httpContext.Response.Body);
    }
}
