using Matrix.BuildingBlocks.Application.Authorization.Jwt;

namespace Matrix.BuildingBlocks.Application.Security.InternalApiKey
{
    public static class InternalApiKeyRingPolicy
    {
        public const string LegacyKeyId = "legacy";

        public static InternalApiKeyResolvedKeyRing Resolve(
            IInternalApiKeyRingOptions options,
            string optionsPath)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            bool hasConfiguredKeyRing =
                !string.IsNullOrWhiteSpace(options.CurrentKeyId) ||
                (options.Keys is { Count: > 0 });

            if (!hasConfiguredKeyRing)
            {
                InternalJwtSigningKeyPolicy.EnsureStrong(
                    signingKey: options.ApiKey,
                    optionsPath: $"{optionsPath}:ApiKey");

                return new InternalApiKeyResolvedKeyRing(
                    currentKeyId: LegacyKeyId,
                    currentApiKey: options.ApiKey,
                    keys: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [LegacyKeyId] = options.ApiKey
                    });
            }

            if (string.IsNullOrWhiteSpace(options.CurrentKeyId))
                throw new InvalidOperationException($"{optionsPath}:CurrentKeyId is required when Keys are configured.");

            if (options.Keys is not { Count: > 0 })
                throw new InvalidOperationException($"{optionsPath}:Keys must contain at least one API key.");

            var normalizedKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach ((string keyId, string apiKey) in options.Keys)
            {
                if (string.IsNullOrWhiteSpace(keyId))
                    throw new InvalidOperationException($"{optionsPath}:Keys contains an empty key id.");

                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new InvalidOperationException($"{optionsPath}:Keys:{keyId} is required.");

                InternalJwtSigningKeyPolicy.EnsureStrong(
                    signingKey: apiKey,
                    optionsPath: $"{optionsPath}:Keys:{keyId}");

                normalizedKeys[keyId] = apiKey;
            }

            if (!normalizedKeys.TryGetValue(
                    key: options.CurrentKeyId,
                    value: out string? currentApiKey))
                throw new InvalidOperationException(
                    $"{optionsPath}:CurrentKeyId '{options.CurrentKeyId}' does not exist in Keys.");

            return new InternalApiKeyResolvedKeyRing(
                currentKeyId: options.CurrentKeyId,
                currentApiKey: currentApiKey,
                keys: normalizedKeys);
        }
    }
}
