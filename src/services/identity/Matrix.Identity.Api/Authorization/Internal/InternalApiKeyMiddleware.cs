using System.Security.Cryptography;
using System.Text;
using Matrix.Identity.Api.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Matrix.Identity.Api.Authorization.Internal
{
    public sealed class InternalApiKeyMiddleware(
        RequestDelegate next,
        IOptions<IdentityInternalOptions> options)
    {
        public const string ApiKeyHeaderName = "X-Internal-Key";
        private readonly IdentityInternalOptions _opts = options.Value;

        public async Task InvokeAsync(HttpContext context)
        {
            // Protect only internal endpoints
            if (!context.Request.Path.StartsWithSegments(
                    other: "/api/internal",
                    comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            // Optional: allow health checks if you want
            // if (context.Request.Path.StartsWithSegments("/api/internal/health")) { ... }

            if (!context.Request.Headers.TryGetValue(
                    key: ApiKeyHeaderName,
                    value: out StringValues providedKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            string expected = _opts.ApiKey;
            if (string.IsNullOrWhiteSpace(expected))
            {
                // Misconfiguration: treat as unauthorized
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            string provided = providedKey.ToString();

            // Constant-time compare
            byte[] providedBytes = Encoding.UTF8.GetBytes(provided);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);

            bool ok = providedBytes.Length == expectedBytes.Length &&
                      CryptographicOperations.FixedTimeEquals(
                          left: providedBytes,
                          right: expectedBytes);

            if (!ok)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            await next(context);
        }
    }
}
