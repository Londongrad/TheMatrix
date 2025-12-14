using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matrix.ApiGateway.Controllers.Common
{
    [Authorize]
    [ApiController]
    public abstract class GatewayControllerBase : ControllerBase
    {
        protected static async Task<ContentResult> ProxyDownstreamErrorAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = body,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
            };
        }

        protected ErrorResponse CreateError(
            string code,
            string message,
            IReadOnlyDictionary<string, string[]>? errors = null)
        {
            return new ErrorResponse(
                Code: code,
                Message: message,
                Errors: errors,
                TraceId: HttpContext.TraceIdentifier);
        }
    }
}
