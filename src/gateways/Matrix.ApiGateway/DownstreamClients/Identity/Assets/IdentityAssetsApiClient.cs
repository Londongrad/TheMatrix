namespace Matrix.ApiGateway.DownstreamClients.Identity.Assets
{
    public sealed class IdentityAssetsApiClient(HttpClient httpClient) : IIdentityAssetsClient
    {
        private readonly HttpClient _httpClient = httpClient;

        public Task<HttpResponseMessage> GetAvatarAsync(
            string fileName,
            CancellationToken ct)
        {
            return _httpClient.GetAsync(
                requestUri: $"/avatars/{fileName}",
                cancellationToken: ct);
        }
    }
}
