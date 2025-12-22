using System.Net;
using System.Text.Json;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.BuildingBlocks.Api.Errors;

namespace Matrix.ApiGateway.DownstreamClients.Common.Errors
{
    public static class DownstreamClientErrorFactory
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new(JsonSerializerDefaults.Web);

        public static DownstreamServiceException InvalidResponseBody(
            string serviceName,
            string? requestUrl,
            string expected)
        {
            var error = new ErrorResponse(
                Code: "Gateway.InvalidDownstreamResponse",
                Message: $"Invalid response body from {serviceName}. Expected: {expected}.",
                Errors: null,
                TraceId: null);

            string body = JsonSerializer.Serialize(error, JsonOptions);

            return new DownstreamServiceException(
                serviceName: serviceName,
                statusCode: HttpStatusCode.BadGateway,
                body: body,
                contentType: "application/json",
                requestUrl: requestUrl);
        }

        public static DownstreamServiceException InvalidJson(
            string serviceName,
            string? requestUrl,
            string expected,
            Exception inner)
        {
            var error = new ErrorResponse(
                Code: "Gateway.InvalidDownstreamJson",
                Message: $"Invalid JSON from {serviceName}. Expected: {expected}.",
                Errors: null,
                TraceId: null);

            string body = JsonSerializer.Serialize(error, JsonOptions);

            return new DownstreamServiceException(
                serviceName: serviceName,
                statusCode: HttpStatusCode.BadGateway,
                body: body,
                contentType: "application/json",
                requestUrl: requestUrl);
        }
    }
}
