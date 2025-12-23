using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Identity.Contracts.Account.Requests;
using Matrix.Identity.Contracts.Account.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Account
{
    public sealed class IdentityAccountApiClient(HttpClient httpClient) : IIdentityAccountClient
    {
        #region [ Fields ]

        private readonly HttpClient _httpClient = httpClient;

        #endregion [ Fields ]

        #region [ Methods ]

        public async Task<UserProfileResponse> GetProfileAsync(CancellationToken ct)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(
                requestUri: ProfileEndpoint,
                cancellationToken: ct);

            return await response.ReadJsonOrThrowDownstreamAsync<UserProfileResponse>(
                serviceName: ServiceName,
                ct: ct,
                requestUrl: ProfileEndpoint);
        }

        public async Task<ChangeAvatarResponse> ChangeAvatarAsync(
            IFormFile avatar,
            CancellationToken ct = default)
        {
            using HttpResponseMessage response = await _httpClient.PutMultipartFileAsync(
                requestUri: AvatarEndpoint,
                formFieldName: "avatar",
                file: avatar,
                ct: ct);

            return await response.ReadJsonOrThrowDownstreamAsync<ChangeAvatarResponse>(
                serviceName: ServiceName,
                ct: ct,
                requestUrl: AvatarEndpoint);
        }

        public async Task ChangePasswordAsync(
            ChangePasswordRequest request,
            CancellationToken ct = default)
        {
            using HttpResponseMessage response = await _httpClient.PutAsJsonAsync(
                requestUri: PasswordEndpoint,
                value: request,
                cancellationToken: ct);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                ct: ct);
        }

        #endregion [ Methods ]

        #region [ Constants ]

        private const string ServiceName = "Identity";

        private const string AccountBaseEndpoint = "/api/account";

        private const string ProfileEndpoint = AccountBaseEndpoint + "/profile";
        private const string AvatarEndpoint = AccountBaseEndpoint + "/avatar";
        private const string PasswordEndpoint = AccountBaseEndpoint + "/password";

        #endregion [ Constants ]
    }
}
