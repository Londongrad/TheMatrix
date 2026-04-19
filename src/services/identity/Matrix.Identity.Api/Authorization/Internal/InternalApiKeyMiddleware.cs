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
            bool requiresInternalKey = context.Request.Path.StartsWithSegments(
                other: "/api/internal",
                comparisonType: StringComparison.OrdinalIgnoreCase);

            if (!context.Request.Headers.TryGetValue(
                    key: ApiKeyHeaderName,
                    value: out StringValues providedKey))
            {
                if (requiresInternalKey)
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                else
                    await next(context);

                return;
            }

            string expected = _opts.ApiKey;
            if (string.IsNullOrWhiteSpace(expected))
            {
                if (requiresInternalKey)
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                else
                    await next(context);

                return;
            }

            string provided = providedKey.ToString();

            byte[] providedBytes = Encoding.UTF8.GetBytes(provided);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);

            bool ok = providedBytes.Length == expectedBytes.Length &&
                      CryptographicOperations.FixedTimeEquals(
                          left: providedBytes,
                          right: expectedBytes);

            // Protect only internal endpoints
            if (!ok)
            {
                if (requiresInternalKey)
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                else
                    await next(context);

                return;
            }

            TrustedGatewayRequestContext.Mark(context);

            await next(context);
        }
    }
}
