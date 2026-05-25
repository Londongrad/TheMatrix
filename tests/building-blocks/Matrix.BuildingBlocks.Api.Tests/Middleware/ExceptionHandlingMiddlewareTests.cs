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

namespace Matrix.BuildingBlocks.Api.Tests.Middleware
{
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

            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: httpContext.Response.StatusCode);
            Assert.Contains(
                expectedSubstring: "\"code\":\"Population.InvalidAge\"",
                actualString: payload,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"detail\":\"Age is invalid.\"",
                actualString: payload,
                comparisonType: StringComparison.Ordinal);
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

            Assert.Equal(
                expected: StatusCodes.Status404NotFound,
                actual: httpContext.Response.StatusCode);
            Assert.Contains(
                expectedSubstring: "\"code\":\"Cities.NotFound\"",
                actualString: payload,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"detail\":\"City not found.\"",
                actualString: payload,
                comparisonType: StringComparison.Ordinal);
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

            Assert.Equal(
                expected: StatusCodes.Status409Conflict,
                actual: httpContext.Response.StatusCode);
            Assert.Contains(
                expectedSubstring: "\"code\":\"Infra.LockFailure\"",
                actualString: payload,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"detail\":\"Operation could not be completed due to a server state conflict.\"",
                actualString: payload,
                comparisonType: StringComparison.Ordinal);
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

            Assert.Equal(
                expected: StatusCodes.Status502BadGateway,
                actual: httpContext.Response.StatusCode);
            Assert.Contains(
                expectedSubstring: "\"code\":\"Economy.DownstreamFailed\"",
                actualString: payload,
                comparisonType: StringComparison.Ordinal);
            Assert.Contains(
                expectedSubstring: "\"detail\":\"Budget service is unavailable.\"",
                actualString: payload,
                comparisonType: StringComparison.Ordinal);
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
}
