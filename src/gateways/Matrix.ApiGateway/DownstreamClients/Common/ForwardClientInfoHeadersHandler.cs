using Microsoft.Extensions.Primitives;

namespace Matrix.ApiGateway.DownstreamClients.Common
{
    public sealed class ForwardClientInfoHeadersHandler(IHttpContextAccessor accessor) : DelegatingHandler
    {
        private readonly IHttpContextAccessor _accessor = accessor;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpContext? ctx = _accessor.HttpContext;

            if (ctx is not null)
            {
                // IP: сначала доверяем заголовку от reverse proxy, иначе берём RemoteIpAddress
                string? ip =
                    ctx.Request.Headers.TryGetValue(
                        key: "X-Real-IP",
                        value: out StringValues xRealIp)
                        ? xRealIp.ToString()
                        : ctx.Connection.RemoteIpAddress?.ToString();

                if (!string.IsNullOrWhiteSpace(ip))
                    request.Headers.TryAddWithoutValidation(
                        name: "X-Real-IP",
                        value: ip);

                // User-Agent
                if (ctx.Request.Headers.TryGetValue(
                        key: "User-Agent",
                        value: out StringValues ua) &&
                    !string.IsNullOrWhiteSpace(ua))
                {
                    request.Headers.Remove("User-Agent");
                    request.Headers.TryAddWithoutValidation(
                        name: "User-Agent",
                        value: ua.ToString());
                }
            }

            return base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
