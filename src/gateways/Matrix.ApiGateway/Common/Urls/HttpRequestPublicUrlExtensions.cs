using Microsoft.AspNetCore.Http.Extensions;

namespace Matrix.ApiGateway.Common.Urls
{
    public static class HttpRequestPublicUrlExtensions
    {
        public static string? ToPublicUrl(
            this HttpRequest request,
            string? urlOrPath)
        {
            if (string.IsNullOrWhiteSpace(urlOrPath))
                return null;

            if (Uri.TryCreate(
                    uriString: urlOrPath,
                    uriKind: UriKind.Absolute,
                    result: out Uri? absoluteUri) &&
                (absoluteUri.Scheme == Uri.UriSchemeHttp ||
                    absoluteUri.Scheme == Uri.UriSchemeHttps))
                return urlOrPath;

            if (!urlOrPath.StartsWith('/'))
                urlOrPath = "/" + urlOrPath;

            // PathBase is part of the public gateway URL when the gateway is mounted under a prefix.
            return UriHelper.BuildAbsolute(
                scheme: request.Scheme,
                host: request.Host,
                pathBase: request.PathBase,
                path: new PathString(urlOrPath));
        }
    }
}
