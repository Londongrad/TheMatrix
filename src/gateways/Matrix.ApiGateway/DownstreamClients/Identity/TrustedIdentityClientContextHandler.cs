using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.BuildingBlocks.Api.Forwarding;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.DownstreamClients.Identity
{
    public sealed class TrustedIdentityClientContextHandler(
        IHttpContextAccessor accessor,
        IOptions<IdentityInternalOptions> options) : DelegatingHandler
    {
        private const string InternalApiKeyHeaderName = "X-Internal-Key";

        private readonly IHttpContextAccessor _accessor = accessor;
        private readonly IdentityInternalOptions _options = options.Value;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Remove(InternalApiKeyHeaderName);
            request.Headers.TryAddWithoutValidation(
                name: InternalApiKeyHeaderName,
                value: _options.ApiKey);

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
