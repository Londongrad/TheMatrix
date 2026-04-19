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

            bool hasConfiguredKeyRing =
                !string.IsNullOrWhiteSpace(options.CurrentKeyId) ||
                (options.Keys is { Count: > 0 });

            if (!hasConfiguredKeyRing)
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

            if (options.Keys is not { Count: > 0 })
                throw new InvalidOperationException($"{optionsPath}:Keys must contain at least one signing key.");

            var normalizedKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach ((string keyId, string signingKey) in options.Keys)
            {
                if (string.IsNullOrWhiteSpace(keyId))
                    throw new InvalidOperationException($"{optionsPath}:Keys contains an empty key id.");

                if (string.IsNullOrWhiteSpace(signingKey))
                    throw new InvalidOperationException($"{optionsPath}:Keys:{keyId} is required.");

                InternalJwtSigningKeyPolicy.EnsureStrong(
                    signingKey: signingKey,
                    optionsPath: $"{optionsPath}:Keys:{keyId}");

                normalizedKeys[keyId] = signingKey;
            }

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
    }
}
