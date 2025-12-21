namespace Matrix.ApiGateway.DownstreamClients.Common
{
    public sealed class ForwardAuthorizationHeaderHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string? auth = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

            if (!string.IsNullOrWhiteSpace(auth))
                request.Headers.TryAddWithoutValidation(
                    name: "Authorization",
                    value: auth);

            return base.SendAsync(
                request: request,
                cancellationToken: cancellationToken);
        }
    }
}
