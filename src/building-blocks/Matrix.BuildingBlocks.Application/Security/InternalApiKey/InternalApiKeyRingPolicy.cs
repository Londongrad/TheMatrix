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

            Dictionary<string, string> normalizedKeys = NormalizeConfiguredKeys(
                keys: options.Keys,
                optionsPath: optionsPath);

            if (normalizedKeys.Count == 0)
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
                throw new InvalidOperationException(
                    $"{optionsPath}:CurrentKeyId is required when Keys are configured.");

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

        private static Dictionary<string, string> NormalizeConfiguredKeys(
            IDictionary<string, string>? keys,
            string optionsPath)
        {
            var normalizedKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            if (keys is null)
                return normalizedKeys;

            foreach ((string keyId, string apiKey) in keys)
            {
                if (string.IsNullOrWhiteSpace(keyId) ||
                    string.IsNullOrWhiteSpace(apiKey))
                    continue;

                InternalJwtSigningKeyPolicy.EnsureStrong(
                    signingKey: apiKey,
                    optionsPath: $"{optionsPath}:Keys:{keyId}");

                normalizedKeys[keyId] = apiKey;
            }

            return normalizedKeys;
        }
    }
}
