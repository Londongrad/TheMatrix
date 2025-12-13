using Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Requests;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Account
{
    public sealed class IdentityAccountApiClient(HttpClient httpClient) : IIdentityAccountClient
    {
        // Базовый префикс для всех эндпоинтов этого контроллера в Identity
        private const string AccountBaseEndpoint = "api/internal/account";

        private const string ProfileSegment = "profile";
        private const string AvatarSegment = "avatar";
        private const string PasswordSegment = "password";

        private readonly HttpClient _httpClient = httpClient;

        public Task<HttpResponseMessage> GetProfileAsync(Guid userId, CancellationToken cancellationToken) =>
            _httpClient.GetAsync(
                requestUri: $"{AccountBaseEndpoint}/{userId:D}/{ProfileSegment}",
                cancellationToken: cancellationToken);

        public async Task<HttpResponseMessage> ChangeAvatarAsync(
            Guid userId,
            IFormFile avatar,
            CancellationToken cancellationToken = default)
        {
            // Если Identity ждёт multipart/form-data
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(avatar.OpenReadStream());
            content.Add(content: streamContent, name: "avatar", fileName: avatar.FileName);

            // /api/internal/account/{userId}/avatar
            string endpoint = $"{AccountBaseEndpoint}/{userId:D}/{AvatarSegment}";

            var message = new HttpRequestMessage(method: HttpMethod.Put, requestUri: endpoint)
            {
                Content = content
            };

            return await _httpClient.SendAsync(request: message, cancellationToken: cancellationToken);
        }

        public async Task<HttpResponseMessage> ChangePasswordAsync(
            Guid userId,
            ChangePasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            // /api/internal/account/{userId}/password
            string endpoint = $"{AccountBaseEndpoint}/{userId:D}/{PasswordSegment}";

            var message = new HttpRequestMessage(method: HttpMethod.Put, requestUri: endpoint)
            {
                Content = JsonContent.Create(request)
            };

            return await _httpClient.SendAsync(request: message, cancellationToken: cancellationToken);
        }
    }
}
