using System.Text.Json;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static Matrix.BuildingBlocks.Api.Tests.TestSupport.BuildingBlocksApiTestSupport;

namespace Matrix.BuildingBlocks.Api.Tests.Errors;

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
        Assert.Equal(StatusCodes.Status400BadRequest, validationProblem.Status);
        Assert.Equal("Users.Invalid", validationProblem.Extensions["code"]);
        Assert.Equal("Validation failed.", validationProblem.Extensions["message"]);
        Assert.Equal("trace-123", validationProblem.Extensions["traceId"]);
        Assert.Equal("/api/users", validationProblem.Instance);
        Assert.Equal(["Required."], validationProblem.Errors["Email"]);
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

        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Contains(ApiProblemDetailsFactory.ProblemContentType, result.ContentTypes);
        ProblemDetails problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal("Users.NotFound", problem.Extensions["code"]);
    }

    [Fact]
    public async Task WriteAsync_WritesProblemDetailsPayloadToResponse()
    {
        DefaultHttpContext httpContext = CreateHttpContext(path: "/api/orders", traceIdentifier: "trace-987");

        await ApiProblemDetailsFactory.WriteAsync(
            context: httpContext,
            statusCode: StatusCodes.Status409Conflict,
            code: "Orders.Conflict",
            message: "Order state conflict.");

        JsonDocument payload = await ReadJsonAsync(httpContext);

        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.Equal(ApiProblemDetailsFactory.ProblemContentType, httpContext.Response.ContentType);
        Assert.Equal("Orders.Conflict", payload.RootElement.GetProperty("code").GetString());
        Assert.Equal("Order state conflict.", payload.RootElement.GetProperty("message").GetString());
        Assert.Equal("trace-987", payload.RootElement.GetProperty("traceId").GetString());
    }
}
