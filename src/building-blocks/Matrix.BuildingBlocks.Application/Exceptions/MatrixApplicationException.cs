using System.Net;

namespace Matrix.BuildingBlocks.Application.Exceptions
{
    /// <summary>
    /// Base exception type for application layer errors in Matrix.
    /// </summary>
    public abstract class MatrixApplicationException(
        string code,
        string message,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        IDictionary<string, string[]>? errors = null) 
        : Exception(message)
    {
        public string Code { get; } = code;
        public HttpStatusCode StatusCode { get; } = statusCode;
        public IDictionary<string, string[]>? Errors { get; } = errors;
    }
}
