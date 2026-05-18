using System.Net;
using System.Text;
using System.Text.Json;
using Matrix.ApiGateway.DownstreamClients.Common.Errors;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.ApiGateway.Tests.Http;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Common;

public sealed class DownstreamHttpHelpersTests
{
    [Fact]
    public async Task EnsureSuccessOrThrowDownstreamAsync_WhenResponseIsFailure_ThrowsWithBodyAndUrl()
    {
        using HttpResponseMessage response = new(HttpStatusCode.BadRequest)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://population.test/api/citizens"),
            Content = new StringContent("{\"code\":\"Validation\"}", Encoding.UTF8, "application/json")
        };

        DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(
            () => response.EnsureSuccessOrThrowDownstreamAsync("Population", CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("Population", exception.ServiceName);
        Assert.Equal("https://population.test/api/citizens", exception.RequestUrl);
        Assert.Contains("Validation", exception.Body);
        Assert.Equal("application/json; charset=utf-8", exception.ContentType);
    }

    [Fact]
    public async Task ReadJsonOrThrowDownstreamAsync_WhenResponseContainsValidJson_ReturnsDto()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://economy.test/api/summary"),
            Content = new StringContent("{\"name\":\"Balanced\"}", Encoding.UTF8, "application/json")
        };

        TestPayload payload = await response.ReadJsonOrThrowDownstreamAsync<TestPayload>(
            serviceName: "Economy",
            cancellationToken: CancellationToken.None);

        Assert.Equal("Balanced", payload.Name);
    }

    [Fact]
    public async Task ReadJsonOrThrowDownstreamAsync_WhenBodyIsNull_ThrowsBadGatewayProblem()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://economy.test/api/summary"),
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(
            () => response.ReadJsonOrThrowDownstreamAsync<TestPayload>(
                serviceName: "Economy",
                cancellationToken: CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Contains("Gateway.InvalidDownstreamResponse", exception.Body);
        Assert.Equal("https://economy.test/api/summary", exception.RequestUrl);
    }

    [Fact]
    public async Task ReadJsonOrThrowDownstreamAsync_WhenJsonIsInvalid_ThrowsBadGatewayProblem()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            RequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://economy.test/api/summary"),
            Content = new StringContent("{bad json", Encoding.UTF8, "application/json")
        };

        DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(
            () => response.ReadJsonOrThrowDownstreamAsync<TestPayload>(
                serviceName: "Economy",
                cancellationToken: CancellationToken.None));

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Contains("Gateway.InvalidDownstreamJson", exception.Body);
    }

    [Fact]
    public async Task SendMultipartFileAsync_WhenPostingFormFile_SendsMultipartBodyAndMethod()
    {
        var handler = new RecordingHttpMessageHandler
        {
            OnSendAsync = (_, _) => Task.FromResult(CreateEmptyResponse())
        };
        using HttpClient client = CreateHttpClient(handler);
        await using MemoryStream stream = new(Encoding.UTF8.GetBytes("hello gateway"));
        FormFile file = new(stream, 0, stream.Length, "avatar", "avatar.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        using HttpResponseMessage response = await client.PostMultipartFileAsync(
            requestUri: "/self/account/avatar",
            formFieldName: "avatar",
            file: file,
            cancellationToken: CancellationToken.None);

        RecordedRequest request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://gateway.test/self/account/avatar", request.RequestUri);
        Assert.StartsWith("multipart/form-data", request.ContentType, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=avatar", request.Body);
        Assert.Contains("filename=avatar.txt", request.Body);
        Assert.Contains("hello gateway", request.Body);
    }

    [Fact]
    public void DownstreamClientErrorFactoryInvalidResponseBody_CreatesBadGatewayProblem()
    {
        DownstreamServiceException exception = DownstreamClientErrorFactory.InvalidResponseBody(
            serviceName: "Resources",
            requestUrl: "https://resources.test/api/stockpiles",
            expected: "CityStockpilesView");

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Equal("https://resources.test/api/stockpiles", exception.RequestUrl);
        Assert.Contains("Gateway.InvalidDownstreamResponse", exception.Body);
        Assert.Contains("CityStockpilesView", exception.Body);
    }

    private sealed record TestPayload(string Name);
}
