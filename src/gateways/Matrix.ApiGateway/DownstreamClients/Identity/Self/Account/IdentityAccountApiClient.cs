using Matrix.ApiGateway.DownstreamClients.Common;
using Matrix.ApiGateway.DownstreamClients.Common.Extensions;
using Matrix.Identity.Contracts.Self.Account.Requests;
using Matrix.Identity.Contracts.Self.Account.Responses;
using Microsoft.AspNetCore.WebUtilities;

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

        public async Task<ChangeDisplayNameResponse> ChangeDisplayNameAsync(
            ChangeDisplayNameRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _httpClient.PutAsJsonAsync(
                requestUri: DisplayNameEndpoint,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<ChangeDisplayNameResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: DisplayNameEndpoint);
        }

        public async Task<ChangeUsernameResponse> ChangeUsernameAsync(
            ChangeUsernameRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _httpClient.PutAsJsonAsync(
                requestUri: UsernameEndpoint,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<ChangeUsernameResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: UsernameEndpoint);
        }

        public async Task<ChangeEmailResponse> ChangeEmailAsync(
            ChangeEmailRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _httpClient.PutAsJsonAsync(
                requestUri: EmailEndpoint,
                value: request,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<ChangeEmailResponse>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: EmailEndpoint);
        }

        public async Task<ChangeAvatarResponse> ClearAvatarAsync(
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _httpClient.DeleteAsync(
                requestUri: AvatarEndpoint,
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

        public async Task DeleteAccountAsync(
            DeleteAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                requestUri: DeleteAccountEndpoint,
                value: request,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);
        }

        public async Task ResendPendingEmailChangeAsync(
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _httpClient.PostAsync(
                requestUri: PendingEmailResendEndpoint,
                content: null,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);
        }

        public async Task CancelPendingEmailChangeAsync(
            CancellationToken cancellationToken = default)
        {
            using HttpResponseMessage response = await _httpClient.DeleteAsync(
                requestUri: PendingEmailEndpoint,
                cancellationToken: cancellationToken);

            await response.EnsureSuccessOrThrowDownstreamAsync(
                serviceName: ServiceName,
                cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyCollection<SecurityActivityItemResponse>> GetSecurityActivityAsync(
            int limit,
            CancellationToken cancellationToken)
        {
            string url = QueryHelpers.AddQueryString(
                uri: SecurityActivityEndpoint,
                queryString: new Dictionary<string, string?>
                {
                    ["limit"] = limit.ToString()
                });

            using HttpResponseMessage response = await _httpClient.GetAsync(
                requestUri: url,
                cancellationToken: cancellationToken);

            return await response.ReadJsonOrThrowDownstreamAsync<IReadOnlyCollection<SecurityActivityItemResponse>>(
                serviceName: ServiceName,
                cancellationToken: cancellationToken,
                requestUrl: url);
        }

        #endregion [ Methods ]

        #region [ Constants ]

        private const string ServiceName = DownstreamServiceNames.Identity;
        private const string AccountBaseEndpoint = "/api/account";

        private const string ProfileEndpoint = AccountBaseEndpoint + "/profile";
        private const string SecurityActivityEndpoint = AccountBaseEndpoint + "/security-activity";
        private const string DisplayNameEndpoint = AccountBaseEndpoint + "/display-name";
        private const string UsernameEndpoint = AccountBaseEndpoint + "/username";
        private const string EmailEndpoint = AccountBaseEndpoint + "/email";
        private const string PendingEmailEndpoint = AccountBaseEndpoint + "/email/pending";
        private const string PendingEmailResendEndpoint = PendingEmailEndpoint + "/resend";
        private const string AvatarEndpoint = AccountBaseEndpoint + "/avatar";
        private const string PasswordEndpoint = AccountBaseEndpoint + "/password";
        private const string DeleteAccountEndpoint = AccountBaseEndpoint + "/delete";

        #endregion [ Constants ]
    }
}
