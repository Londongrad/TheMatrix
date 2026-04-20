using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.BuildingBlocks.Api.Forwarding;
using Microsoft.Extensions.Primitives;

namespace Matrix.ApiGateway.DownstreamClients.Identity
{
    public sealed class TrustedIdentityClientContextHandler(
        IHttpContextAccessor accessor) : DelegatingHandler
    {
        private readonly IHttpContextAccessor _accessor = accessor;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpContext? ctx = _accessor.HttpContext;
            if (ctx is not null)
            {
                string? clientIp = ctx.GetNormalizedClientIpAddress();
                if (!string.IsNullOrWhiteSpace(clientIp))
                {
                    request.Headers.Remove(TrustedGatewayClientHeaders.ClientIpHeaderName);
                    request.Headers.TryAddWithoutValidation(
                        name: TrustedGatewayClientHeaders.ClientIpHeaderName,
                        value: clientIp);
                }

                if (ctx.Request.Headers.TryGetValue(
                        key: "User-Agent",
                        value: out StringValues userAgent) &&
                    !string.IsNullOrWhiteSpace(userAgent))
                {
                    request.Headers.Remove("User-Agent");
                    request.Headers.TryAddWithoutValidation(
                        name: "User-Agent",
                        value: userAgent.ToString());
                }
            }

            return base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
