using System.Net;
using Matrix.BuildingBlocks.Api.Exceptions;
using Matrix.BuildingBlocks.Api.Middleware;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Matrix.BuildingBlocks.Api.Tests.TestSupport.BuildingBlocksApiTestSupport;

namespace Matrix.BuildingBlocks.Api.Tests.Middleware;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Invoke_WhenDomainExceptionIsThrown_WritesBadRequestProblemWithFieldErrors()
    {
        DefaultHttpContext httpContext = CreateHttpContext(path: "/api/people");
        var middleware = new ExceptionHandlingMiddleware(
            next: _ => throw new DomainException(
                code: "Population.InvalidAge",
                message: "Age is invalid.",
                propertyName: "Age"),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.Invoke(httpContext);

        string payload = await ReadBodyAsStringAsync(httpContext);

        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.Contains("\"code\":\"Population.InvalidAge\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"detail\":\"Age is invalid.\"", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_WhenApplicationExceptionIsThrown_MapsExpectedStatusAndErrors()
    {
        DefaultHttpContext httpContext = CreateHttpContext(path: "/api/cities");
        var middleware = new ExceptionHandlingMiddleware(
            next: _ => throw new MatrixApplicationException(
                code: "Cities.NotFound",
                message: "City not found.",
                errorType: ApplicationErrorType.NotFound,
                errors: new Dictionary<string, string[]>
                {
                    ["CityId"] = ["Unknown city."]
                }),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.Invoke(httpContext);

        string payload = await ReadBodyAsStringAsync(httpContext);

        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
        Assert.Contains("\"code\":\"Cities.NotFound\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"detail\":\"City not found.\"", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_WhenInfrastructureExceptionIsThrown_HidesInternalMessageBehindPublicConflictMessage()
    {
        DefaultHttpContext httpContext = CreateHttpContext(path: "/api/budget");
        var middleware = new ExceptionHandlingMiddleware(
            next: _ => throw new MatrixInfrastructureException(
                code: "Infra.LockFailure",
                message: "Redis lock table is corrupted.",
                errorType: ApplicationErrorType.Conflict),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.Invoke(httpContext);

        string payload = await ReadBodyAsStringAsync(httpContext);

        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.Contains("\"code\":\"Infra.LockFailure\"", payload, StringComparison.Ordinal);
        Assert.Contains(
            "\"detail\":\"Operation could not be completed due to a server state conflict.\"",
            payload,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_WhenDownstreamProblemJsonIsThrown_SanitizesAndPropagatesPayload()
    {
        DefaultHttpContext httpContext = CreateHttpContext(path: "/api/economy");
        var middleware = new ExceptionHandlingMiddleware(
            next: _ => throw new TestHttpResponseException(
                statusCode: HttpStatusCode.BadGateway,
                contentType: "application/problem+json",
                body:
                """
                {
                  "code":"Economy.DownstreamFailed",
                  "detail":"Budget service is unavailable.",
                  "errors":{"Phase":["retry later"]}
                }
                """),
            logger: NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.Invoke(httpContext);

        string payload = await ReadBodyAsStringAsync(httpContext);

        Assert.Equal(StatusCodes.Status502BadGateway, httpContext.Response.StatusCode);
        Assert.Contains("\"code\":\"Economy.DownstreamFailed\"", payload, StringComparison.Ordinal);
        Assert.Contains("\"detail\":\"Budget service is unavailable.\"", payload, StringComparison.Ordinal);
    }

    private sealed class TestHttpResponseException(
        HttpStatusCode statusCode,
        string? contentType,
        string? body,
        string? serviceName = "economy",
        string? requestUrl = "https://economy.test/api/budget")
        : Exception("downstream failed"), IHttpResponseException
    {
        public HttpStatusCode StatusCode { get; } = statusCode;
        public string? ContentType { get; } = contentType;
        public string? Body { get; } = body;
        public string? ServiceName { get; } = serviceName;
        public string? RequestUrl { get; } = requestUrl;
    }
}
