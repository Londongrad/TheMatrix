using System.Security.Cryptography;
using System.Text;
using Matrix.BuildingBlocks.Application.Security.InternalApiKey;
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
        public const string ApiKeyIdHeaderName = "X-Internal-Key-Id";

        private readonly InternalApiKeyResolvedKeyRing _keyRing = InternalApiKeyRingPolicy.Resolve(
            options: options.Value,
            optionsPath: IdentityInternalOptions.SectionName);

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

            string provided = providedKey.ToString();
            bool ok = TryValidateProvidedKey(
                context: context,
                providedKey: provided);

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

        private bool TryValidateProvidedKey(
            HttpContext context,
            string providedKey)
        {
            if (context.Request.Headers.TryGetValue(
                    key: ApiKeyIdHeaderName,
                    value: out StringValues keyIdValues))
            {
                string? keyId = keyIdValues.ToString();
                if (!string.IsNullOrWhiteSpace(keyId) &&
                    _keyRing.Keys.TryGetValue(
                        key: keyId,
                        value: out string? expectedKey))
                    return FixedTimeEquals(
                        provided: providedKey,
                        expected: expectedKey);
            }

            foreach (string expectedKey in _keyRing.Keys.Values)
                if (FixedTimeEquals(
                        provided: providedKey,
                        expected: expectedKey))
                    return true;

            return false;
        }

        private static bool FixedTimeEquals(
            string provided,
            string expected)
        {
            byte[] providedBytes = Encoding.UTF8.GetBytes(provided);
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);

            return providedBytes.Length == expectedBytes.Length &&
                   CryptographicOperations.FixedTimeEquals(
                       left: providedBytes,
                       right: expectedBytes);
        }
    }
}
