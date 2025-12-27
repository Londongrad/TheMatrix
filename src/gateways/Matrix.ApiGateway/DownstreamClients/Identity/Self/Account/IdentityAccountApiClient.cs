using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Identity.Contracts.Self.Account.Requests;
using Matrix.Identity.Contracts.Self.Account.Responses;

namespace Matrix.ApiGateway.DownstreamClients.Identity.Self.Account
{
    public sealed class IdentityAccountApiClient(HttpClient httpClient) : IIdentityAccountClient
    {
        #region [ Fields ]

        private readonly HttpClient _httpClient = httpClient;

        #endregion [ Fields ]

        #region [ Methods ]

        public async Task<UserProfileResponse> GetProfileAsync(CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(
                requestUri: ProfileEndpoint,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<UserProfileResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: ProfileEndpoint);
        }

        public async Task<ChangeAvatarResponse> ChangeAvatarAsync(
            IFormFile avatar,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _httpClient.PutMultipartFileAsync(
                requestUri: AvatarEndpoint,
                formFieldName: "avatar",
                file: avatar,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<ChangeAvatarResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: AvatarEndpoint);
        }

        public async Task ChangePasswordAsync(
            ChangePasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _httpClient.PutAsJsonAsync(
                requestUri: PasswordEndpoint,
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);
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
