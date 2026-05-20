using Matrix.BuildingBlocks.Api.Errors;
using Microsoft.Extensions.Options;

namespace Matrix.ApiGateway.Configurations.Security
{
    public sealed class BrowserCookieRequestProtectionMiddleware(
        RequestDelegate next,
        IOptions<FrontendSecurityOptions> options)
    {
        private static readonly PathString RefreshPath = new("/api/auth/refresh");
        private static readonly PathString LogoutPath = new("/api/auth/logout");
        private readonly FrontendSecurityOptions _options = options.Value;

        public async Task InvokeAsync(HttpContext context)
        {
            if (!ShouldProtect(context))
            {
                await next(context);
                return;
            }

            if (IsExplicitCrossSite(context))
            {
                await WriteRejectedResponseAsync(
                    context,
                    code: "Gateway.CrossSiteCookieRequestRejected",
                    message: "Cross-site cookie request rejected.");
                return;
            }

            if (!TryResolveRequestOrigin(
                    context: context,
                    origin: out string? requestOrigin))
            {
                await WriteRejectedResponseAsync(
                    context,
                    code: "Gateway.UnverifiableCookieRequestOrigin",
                    message: "Cookie request origin could not be verified.");
                return;
            }

            if (IsAllowedOrigin(
                    context: context,
                    origin: requestOrigin))
            {
                await next(context);
                return;
            }

            await WriteRejectedResponseAsync(
                context,
                code: "Gateway.UntrustedCookieRequestOrigin",
                message: "Cookie request origin is not trusted.");
        }

        private bool ShouldProtect(HttpContext context)
        {
            if (!_options.EnforceCookieOriginProtection)
                return false;

            if (!HttpMethods.IsPost(context.Request.Method))
                return false;

            PathString path = context.Request.Path;
            return path.Equals(RefreshPath) || path.Equals(LogoutPath);
        }

        private static bool IsExplicitCrossSite(HttpContext context)
        {
            string? fetchSite = context.Request.Headers["Sec-Fetch-Site"].FirstOrDefault();
            return string.Equals(
                a: fetchSite,
                b: "cross-site",
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryResolveRequestOrigin(
            HttpContext context,
            out string? origin)
        {
            origin = null;

            string? requestOrigin = context.Request.Headers.Origin.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(requestOrigin))
            {
                return TryNormalizeOrigin(
                    candidate: requestOrigin,
                    origin: out origin);
            }

            string? referer = context.Request.Headers.Referer.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(referer))
            {
                return TryNormalizeOrigin(
                    candidate: referer,
                    origin: out origin);
            }

            return false;
        }

        private bool IsAllowedOrigin(
            HttpContext context,
            string? origin)
        {
            if (string.IsNullOrWhiteSpace(origin))
                return false;

            string requestHostOrigin = $"{context.Request.Scheme}://{context.Request.Host.Value}";
            if (string.Equals(
                    a: requestHostOrigin,
                    b: origin,
                    comparisonType: StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (string allowedOrigin in _options.AllowedOrigins)
            {
                if (!TryNormalizeOrigin(
                        candidate: allowedOrigin,
                        origin: out string? normalizedAllowedOrigin))
                    continue;

                if (string.Equals(
                        a: normalizedAllowedOrigin,
                        b: origin,
                        comparisonType: StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool TryNormalizeOrigin(
            string? candidate,
            out string? origin)
        {
            origin = null;

            if (string.IsNullOrWhiteSpace(candidate))
                return false;

            if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri))
                return false;

            origin = uri.GetLeftPart(UriPartial.Authority);
            return true;
        }

        private static async Task WriteRejectedResponseAsync(
            HttpContext context,
            string code,
            string message)
        {
            await ApiProblemDetailsFactory.WriteAsync(
                context: context,
                statusCode: StatusCodes.Status403Forbidden,
                code: code,
                message: message,
                cancellationToken: context.RequestAborted);
        }
    }
}
