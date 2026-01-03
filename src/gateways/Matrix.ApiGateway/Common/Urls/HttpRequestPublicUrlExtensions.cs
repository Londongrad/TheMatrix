using Microsoft.AspNetCore.Http.Extensions;

namespace Matrix.ApiGateway.Common.Urls
{
    public static class HttpRequestPublicUrlExtensions
    {
        public static string? ToPublicUrl(this HttpRequest request, string? urlOrPath)
        {
            if (string.IsNullOrWhiteSpace(urlOrPath))
                return null;

            if (Uri.TryCreate(urlOrPath, UriKind.Absolute, out _))
                return urlOrPath;

            if (!urlOrPath.StartsWith('/'))
                urlOrPath = "/" + urlOrPath;

            // Важно: PathBase учтётся (если гейтвей висит не в корне домена)
            return UriHelper.BuildAbsolute(
                scheme: request.Scheme,
                host: request.Host,
                pathBase: request.PathBase,
                path: new PathString(urlOrPath));
        }
    }
}
