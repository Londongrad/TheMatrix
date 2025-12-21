using System.Net;
using Matrix.BuildingBlocks.Api.Exceptions;

namespace Matrix.ApiGateway.DownstreamClients.Common.Exceptions
{
    public sealed class DownstreamServiceException(
        string serviceName,
        HttpStatusCode statusCode,
        string? body,
        string? contentType,
        string? requestUrl)
        : Exception(
                $"Downstream call failed. Service={serviceName}. Status={(int)statusCode} ({statusCode}). Url={requestUrl}"),
            IHttpResponseException
    {
        public HttpStatusCode StatusCode { get; } = statusCode;
        public string? ContentType { get; } = contentType;
        public string? Body { get; } = body;
        public string? ServiceName { get; } = serviceName;
        public string? RequestUrl { get; } = requestUrl;
    }
}
