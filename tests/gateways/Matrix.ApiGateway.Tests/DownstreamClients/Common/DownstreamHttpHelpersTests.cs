using System.Net;
using System.Text;
using Matrix.ApiGateway.DownstreamClients.Common.Errors;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.ApiGateway.Tests.Http.HttpClientTestSupport;

namespace Matrix.ApiGateway.Tests.DownstreamClients.Common
{
    public sealed class DownstreamHttpHelpersTests
    {
        [Fact]
        public async Task EnsureSuccessOrThrowDownstreamAsync_WhenResponseIsFailure_ThrowsWithBodyAndUrl()
        {
            using HttpResponseMessage response = new(HttpStatusCode.BadRequest)
            {
                RequestMessage = new HttpRequestMessage(
                    method: HttpMethod.Get,
                    requestUri: "https://population.test/api/citizens"),
                Content = new StringContent(
                    content: "{\"code\":\"Validation\"}",
                    encoding: Encoding.UTF8,
                    mediaType: "application/json")
            };

            DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(()
                => response.EnsureSuccessOrThrowDownstreamAsync(
                    serviceName: "Population",
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: HttpStatusCode.BadRequest,
                actual: exception.StatusCode);
            Assert.Equal(
                expected: "Population",
                actual: exception.ServiceName);
            Assert.Equal(
                expected: "https://population.test/api/citizens",
                actual: exception.RequestUrl);
            Assert.Contains(
                expectedSubstring: "Validation",
                actualString: exception.Body);
            Assert.Equal(
                expected: "application/json; charset=utf-8",
                actual: exception.ContentType);
        }

        [Fact]
        public async Task ReadJsonOrThrowDownstreamAsync_WhenResponseContainsValidJson_ReturnsDto()
        {
            using HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage(
                    method: HttpMethod.Get,
                    requestUri: "https://economy.test/api/summary"),
                Content = new StringContent(
                    content: "{\"name\":\"Balanced\"}",
                    encoding: Encoding.UTF8,
                    mediaType: "application/json")
            };

            TestPayload payload = await response.ReadJsonOrThrowDownstreamAsync<TestPayload>(
                serviceName: "Economy",
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: "Balanced",
                actual: payload.Name);
        }

        [Fact]
        public async Task ReadJsonOrThrowDownstreamAsync_WhenBodyIsNull_ThrowsBadGatewayProblem()
        {
            using HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage(
                    method: HttpMethod.Get,
                    requestUri: "https://economy.test/api/summary"),
                Content = new StringContent(
                    content: "null",
                    encoding: Encoding.UTF8,
                    mediaType: "application/json")
            };

            DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(()
                => response.ReadJsonOrThrowDownstreamAsync<TestPayload>(
                    serviceName: "Economy",
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: HttpStatusCode.BadGateway,
                actual: exception.StatusCode);
            Assert.Contains(
                expectedSubstring: "Gateway.InvalidDownstreamResponse",
                actualString: exception.Body);
            Assert.Equal(
                expected: "https://economy.test/api/summary",
                actual: exception.RequestUrl);
        }

        [Fact]
        public async Task ReadJsonOrThrowDownstreamAsync_WhenJsonIsInvalid_ThrowsBadGatewayProblem()
        {
            using HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage(
                    method: HttpMethod.Get,
                    requestUri: "https://economy.test/api/summary"),
                Content = new StringContent(
                    content: "{bad json",
                    encoding: Encoding.UTF8,
                    mediaType: "application/json")
            };

            DownstreamServiceException exception = await Assert.ThrowsAsync<DownstreamServiceException>(()
                => response.ReadJsonOrThrowDownstreamAsync<TestPayload>(
                    serviceName: "Economy",
                    cancellationToken: CancellationToken.None));

            Assert.Equal(
                expected: HttpStatusCode.BadGateway,
                actual: exception.StatusCode);
            Assert.Contains(
                expectedSubstring: "Gateway.InvalidDownstreamJson",
                actualString: exception.Body);
        }

        [Fact]
        public async Task SendMultipartFileAsync_WhenPostingFormFile_SendsMultipartBodyAndMethod()
        {
            var handler = new RecordingHttpMessageHandler
            {
                OnSendAsync = (
                    _,
                    _) => Task.FromResult(CreateEmptyResponse())
            };
            using HttpClient client = CreateHttpClient(handler);
            await using MemoryStream stream = new(Encoding.UTF8.GetBytes("hello gateway"));
            FormFile file = new(
                baseStream: stream,
                baseStreamOffset: 0,
                length: stream.Length,
                name: "avatar",
                fileName: "avatar.txt")
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
            Assert.Equal(
                expected: HttpMethod.Post,
                actual: request.Method);
            Assert.Equal(
                expected: "https://gateway.test/self/account/avatar",
                actual: request.RequestUri);
            Assert.StartsWith(
                expectedStartString: "multipart/form-data",
                actualString: request.ContentType,
                comparisonType: StringComparison.OrdinalIgnoreCase);
            Assert.Contains(
                expectedSubstring: "name=avatar",
                actualString: request.Body);
            Assert.Contains(
                expectedSubstring: "filename=avatar.txt",
                actualString: request.Body);
            Assert.Contains(
                expectedSubstring: "hello gateway",
                actualString: request.Body);
        }

        [Fact]
        public void DownstreamClientErrorFactoryInvalidResponseBody_CreatesBadGatewayProblem()
        {
            DownstreamServiceException exception = DownstreamClientErrorFactory.InvalidResponseBody(
                serviceName: "Resources",
                requestUrl: "https://resources.test/api/stockpiles",
                expected: "CityStockpilesView");

            Assert.Equal(
                expected: HttpStatusCode.BadGateway,
                actual: exception.StatusCode);
            Assert.Equal(
                expected: "https://resources.test/api/stockpiles",
                actual: exception.RequestUrl);
            Assert.Contains(
                expectedSubstring: "Gateway.InvalidDownstreamResponse",
                actualString: exception.Body);
            Assert.Contains(
                expectedSubstring: "CityStockpilesView",
                actualString: exception.Body);
        }

        private sealed record TestPayload(string Name);
    }
}
