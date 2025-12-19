using System.Net;

namespace Matrix.BuildingBlocks.Api.Exceptions
{
    public interface IHttpResponseException
    {
        HttpStatusCode StatusCode { get; }
        string? ContentType { get; }
        string? Body { get; }

        // опционально, но удобно для логов
        string? ServiceName { get; }
        string? RequestUrl { get; }
    }
}
