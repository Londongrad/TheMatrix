using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Account
{
    public sealed class IdentityAccountApiClient(HttpClient httpClient) : IIdentityAccountClient
    {
        private const string ChangeAvatarEndpoint = "api/account/avatar";
        private const string ChangePasswordEndpoint = "api/account/password";
        private readonly HttpClient _httpClient = httpClient;

        public async Task<HttpResponseMessage> ChangeAvatarAsync(
            string authorizationHeader,
            ChangeAvatarRequest request,
            CancellationToken ct = default)
        {
            var message = new HttpRequestMessage(method: HttpMethod.Put, requestUri: ChangeAvatarEndpoint)
            {
                Content = JsonContent.Create(request)
            };

            if (!string.IsNullOrWhiteSpace(authorizationHeader))
                message.Headers.TryAddWithoutValidation(name: "Authorization", value: authorizationHeader);

            return await _httpClient.SendAsync(request: message, cancellationToken: ct);
        }

        public async Task<HttpResponseMessage> ChangePasswordAsync(
            string authorizationHeader,
            ChangePasswordRequest request,
            CancellationToken ct = default)
        {
            var message = new HttpRequestMessage(method: HttpMethod.Put, requestUri: ChangePasswordEndpoint)
            {
                Content = JsonContent.Create(request)
            };

            if (!string.IsNullOrWhiteSpace(authorizationHeader))
                message.Headers.TryAddWithoutValidation(name: "Authorization", value: authorizationHeader);

            return await _httpClient.SendAsync(request: message, cancellationToken: ct);
        }
    }
}
