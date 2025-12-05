using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Account
{
    public sealed class IdentityAccountApiClient(HttpClient httpClient) : IIdentityAccountClient
    {
        private readonly HttpClient _httpClient = httpClient;

        private const string ChangeAvatarEndpoint = "api/account/avatar";
        private const string ChangePasswordEndpoint = "api/account/password";

        public async Task<HttpResponseMessage> ChangeAvatarAsync(
            string authorizationHeader,
            ChangeAvatarRequest request,
            CancellationToken ct = default)
        {
            var message = new HttpRequestMessage(HttpMethod.Put, ChangeAvatarEndpoint)
            {
                Content = JsonContent.Create(request)
            };

            if (!string.IsNullOrWhiteSpace(authorizationHeader))
                message.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);

            return await _httpClient.SendAsync(message, ct);
        }

        public async Task<HttpResponseMessage> ChangePasswordAsync(
            string authorizationHeader,
            ChangePasswordRequest request,
            CancellationToken ct = default)
        {
            var message = new HttpRequestMessage(HttpMethod.Put, ChangePasswordEndpoint)
            {
                Content = JsonContent.Create(request)
            };

            if (!string.IsNullOrWhiteSpace(authorizationHeader))
                message.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);

            return await _httpClient.SendAsync(message, ct);
        }
    }
}