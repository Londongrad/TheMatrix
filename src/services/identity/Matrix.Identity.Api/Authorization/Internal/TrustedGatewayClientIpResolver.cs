using Matrix.BuildingBlocks.Api.Forwarding;
using Microsoft.Extensions.Primitives;

namespace Matrix.Identity.Api.Authorization.Internal
{
    public static class TrustedGatewayClientIpResolver
    {
        public static string? Resolve(HttpContext context)
        {
            if (TrustedGatewayRequestContext.IsTrusted(context) &&
                context.Request.Headers.TryGetValue(
                    key: TrustedGatewayClientHeaders.ClientIpHeaderName,
                    value: out StringValues forwardedClientIp))
            {
                string? trustedIp = TrustedForwardedHeadersExtensions.NormalizeClientIpAddress(
                    value: forwardedClientIp.ToString());

                if (!string.IsNullOrWhiteSpace(trustedIp))
                    return trustedIp;
            }

            return context.GetNormalizedClientIpAddress();
        }
    }
}
