namespace Matrix.BuildingBlocks.Application.Authorization.Jwt
{
    public static class InternalJwtKeyRingPolicy
    {
        public const string LegacyKeyId = "legacy";

        public static InternalJwtResolvedKeyRing Resolve(
            IInternalJwtKeyRingOptions options,
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
                    signingKey: options.SigningKey,
                    optionsPath: $"{optionsPath}:SigningKey");

                return new InternalJwtResolvedKeyRing(
                    currentKeyId: LegacyKeyId,
                    currentSigningKey: options.SigningKey,
                    keys: new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        [LegacyKeyId] = options.SigningKey
                    });
            }

            if (string.IsNullOrWhiteSpace(options.CurrentKeyId))
                throw new InvalidOperationException($"{optionsPath}:CurrentKeyId is required when Keys are configured.");

            if (!normalizedKeys.TryGetValue(
                    key: options.CurrentKeyId,
                    value: out string? currentSigningKey))
                throw new InvalidOperationException(
                    $"{optionsPath}:CurrentKeyId '{options.CurrentKeyId}' does not exist in Keys.");

            return new InternalJwtResolvedKeyRing(
                currentKeyId: options.CurrentKeyId,
                currentSigningKey: currentSigningKey,
                keys: normalizedKeys);
        }

        public static bool HasConfiguredKeyRing(IInternalJwtKeyRingOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            return NormalizeConfiguredKeys(
                keys: options.Keys,
                optionsPath: null).Count > 0;
        }

        private static Dictionary<string, string> NormalizeConfiguredKeys(
            IDictionary<string, string>? keys,
            string? optionsPath)
        {
            var normalizedKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            if (keys is null)
                return normalizedKeys;

            foreach ((string keyId, string signingKey) in keys)
            {
                if (string.IsNullOrWhiteSpace(keyId) ||
                    string.IsNullOrWhiteSpace(signingKey))
                {
                    continue;
                }

                InternalJwtSigningKeyPolicy.EnsureStrong(
                    signingKey: signingKey,
                    optionsPath: $"{optionsPath ?? "Keys"}:{keyId}");

                normalizedKeys[keyId] = signingKey;
            }

            return normalizedKeys;
        }
    }
}
