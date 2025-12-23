using System.Net.Http.Headers;

namespace Matrix.ApiGateway.DownstreamClients.Common
{
    public sealed class ForwardAuthorizationHeaderHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string? raw = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(raw) &&
                AuthenticationHeaderValue.TryParse(
                    input: raw,
                    parsedValue: out AuthenticationHeaderValue? headerValue))
                request.Headers.Authorization = headerValue;

            return base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
