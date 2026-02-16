using Matrix.Identity.Application.Abstractions.Services;
using Microsoft.Extensions.Options;

namespace Matrix.Identity.Infrastructure.Integration.Links
{
    public sealed class FrontendLinkBuilder(
        IOptions<FrontendLinksOptions> options) : IFrontendLinkBuilder
    {
        public string BuildConfirmEmailLink(
            Guid userId,
            string rawToken)
        {
            return BuildLink(
                path: options.Value.ConfirmEmailPath,
                userId: userId,
                rawToken: rawToken);
        }

        public string BuildResetPasswordLink(
            Guid userId,
            string rawToken)
        {
            return BuildLink(
                path: options.Value.ResetPasswordPath,
                userId: userId,
                rawToken: rawToken);
        }

        private string BuildLink(
            string path,
            Guid userId,
            string rawToken)
        {
            var baseUri = new Uri(options.Value.BaseUrl, UriKind.Absolute);
            string normalizedPath = path.StartsWith('/')
                ? path[1..]
                : path;

            var targetUri = new Uri(baseUri, normalizedPath);
            var builder = new UriBuilder(targetUri)
            {
                Query =
                    $"userId={Uri.EscapeDataString(userId.ToString())}&token={Uri.EscapeDataString(rawToken)}"
            };

            return builder.Uri.ToString();
        }
    }
}
