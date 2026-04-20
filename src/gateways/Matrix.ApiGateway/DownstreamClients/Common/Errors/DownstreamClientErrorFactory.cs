using System.Net;
using System.Text.Json;
using Matrix.ApiGateway.DownstreamClients.Common.Exceptions;
using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Mvc;

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
            ProblemDetails error = ApiProblemDetailsFactory.Create(
                statusCode: (int)HttpStatusCode.BadGateway,
                code: "Gateway.InvalidDownstreamResponse",
                message: $"Invalid response body from {serviceName}. Expected: {expected}.");

            string body = JsonSerializer.Serialize(
                value: error,
                options: JsonOptions);

            return new DownstreamServiceException(
                serviceName: serviceName,
                statusCode: HttpStatusCode.BadGateway,
                body: body,
                contentType: ApiProblemDetailsFactory.ProblemContentType,
                requestUrl: requestUrl);
        }

        public static DownstreamServiceException InvalidJson(
            string serviceName,
            string? requestUrl,
            string expected,
            Exception inner)
        {
            ProblemDetails error = ApiProblemDetailsFactory.Create(
                statusCode: (int)HttpStatusCode.BadGateway,
                code: "Gateway.InvalidDownstreamJson",
                message: $"Invalid JSON from {serviceName}. Expected: {expected}.");

            string body = JsonSerializer.Serialize(
                value: error,
                options: JsonOptions);

            return new DownstreamServiceException(
                serviceName: serviceName,
                statusCode: HttpStatusCode.BadGateway,
                body: body,
                contentType: ApiProblemDetailsFactory.ProblemContentType,
                requestUrl: requestUrl);
        }
    }
}
