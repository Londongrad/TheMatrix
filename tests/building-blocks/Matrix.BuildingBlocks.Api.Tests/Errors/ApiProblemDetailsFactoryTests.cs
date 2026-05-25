using System.Text.Json;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.BuildingBlocks.Api.Tests.TestSupport.BuildingBlocksApiTestSupport;

namespace Matrix.BuildingBlocks.Api.Tests.Errors
{
    public sealed class ApiProblemDetailsFactoryTests
    {
        [Fact]
        public void Create_WhenValidationErrorsAreProvided_ReturnsValidationProblemDetailsWithExtensions()
        {
            DefaultHttpContext httpContext = CreateHttpContext(path: "/api/users");

            ProblemDetails problem = ApiProblemDetailsFactory.Create(
                context: httpContext,
                statusCode: StatusCodes.Status400BadRequest,
                code: "Users.Invalid",
                message: "Validation failed.",
                errors: new Dictionary<string, string[]>
                {
                    ["Email"] = ["Required."]
                });

            ValidationProblemDetails validationProblem = Assert.IsType<ValidationProblemDetails>(problem);
            Assert.Equal(
                expected: StatusCodes.Status400BadRequest,
                actual: validationProblem.Status);
            Assert.Equal(
                expected: "Users.Invalid",
                actual: validationProblem.Extensions["code"]);
            Assert.Equal(
                expected: "Validation failed.",
                actual: validationProblem.Extensions["message"]);
            Assert.Equal(
                expected: "trace-123",
                actual: validationProblem.Extensions["traceId"]);
            Assert.Equal(
                expected: "/api/users",
                actual: validationProblem.Instance);
            Assert.Equal(
                expectedSpan: ["Required."],
                actualArray: validationProblem.Errors["Email"]);
        }

        [Fact]
        public void CreateObjectResult_SetsStatusAndProblemContentType()
        {
            DefaultHttpContext httpContext = CreateHttpContext();

            ObjectResult result = ApiProblemDetailsFactory.CreateObjectResult(
                context: httpContext,
                statusCode: StatusCodes.Status404NotFound,
                code: "Users.NotFound",
                message: "User was not found.");

            Assert.Equal(
                expected: StatusCodes.Status404NotFound,
                actual: result.StatusCode);
            Assert.Contains(
                expected: ApiProblemDetailsFactory.ProblemContentType,
                collection: result.ContentTypes);
            ProblemDetails problem = Assert.IsType<ProblemDetails>(result.Value);
            Assert.Equal(
                expected: "Users.NotFound",
                actual: problem.Extensions["code"]);
        }

        [Fact]
        public async Task WriteAsync_WritesProblemDetailsPayloadToResponse()
        {
            DefaultHttpContext httpContext = CreateHttpContext(
                path: "/api/orders",
                traceIdentifier: "trace-987");

            await ApiProblemDetailsFactory.WriteAsync(
                context: httpContext,
                statusCode: StatusCodes.Status409Conflict,
                code: "Orders.Conflict",
                message: "Order state conflict.");

            JsonDocument payload = await ReadJsonAsync(httpContext);

            Assert.Equal(
                expected: StatusCodes.Status409Conflict,
                actual: httpContext.Response.StatusCode);
            Assert.Equal(
                expected: ApiProblemDetailsFactory.ProblemContentType,
                actual: httpContext.Response.ContentType);
            Assert.Equal(
                expected: "Orders.Conflict",
                actual: payload.RootElement.GetProperty("code")
                   .GetString());
            Assert.Equal(
                expected: "Order state conflict.",
                actual: payload.RootElement.GetProperty("message")
                   .GetString());
            Assert.Equal(
                expected: "trace-987",
                actual: payload.RootElement.GetProperty("traceId")
                   .GetString());
        }
    }
}
