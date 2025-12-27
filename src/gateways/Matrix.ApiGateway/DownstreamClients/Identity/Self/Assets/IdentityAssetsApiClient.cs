namespace Matrix.ApiGateway.DownstreamClients.Identity.Self.Assets
{
    public sealed class IdentityAssetsApiClient(HttpClient httpClient) : IIdentityAssetsClient
    {
        private readonly HttpClient _httpClient = httpClient;

        public Task<HttpResponseMessage> GetAvatarAsync(
            string fileName,
            CancellationToken cancellationToken)
        {
            return _httpClient.GetAsync(
                requestUri: $"/avatars/{fileName}",
                cancellationToken: cancellationToken);
        }
    }
}
