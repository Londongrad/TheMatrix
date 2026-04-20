using Matrix.ApiGateway.Authorization.PermissionsVersion.Options;
using Matrix.BuildingBlocks.Application.Security.InternalApiKey;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.DownstreamClients.Identity
{
    public sealed class InternalIdentityApiKeyAuthenticationHandler(
        IOptions<IdentityInternalOptions> options) : DelegatingHandler
    {
        public const string InternalApiKeyHeaderName = "X-Internal-Key";
        public const string InternalApiKeyIdHeaderName = "X-Internal-Key-Id";

        private readonly InternalApiKeyResolvedKeyRing _keyRing = InternalApiKeyRingPolicy.Resolve(
            options: options.Value,
            optionsPath: IdentityInternalOptions.SectionName);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Remove(InternalApiKeyHeaderName);
            request.Headers.Remove(InternalApiKeyIdHeaderName);
            request.Headers.TryAddWithoutValidation(
                name: InternalApiKeyIdHeaderName,
                value: _keyRing.CurrentKeyId);
            request.Headers.TryAddWithoutValidation(
                name: InternalApiKeyHeaderName,
                value: _keyRing.CurrentApiKey);

            return base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
