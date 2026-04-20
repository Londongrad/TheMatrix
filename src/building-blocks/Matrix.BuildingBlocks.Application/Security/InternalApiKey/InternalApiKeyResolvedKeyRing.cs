namespace Matrix.BuildingBlocks.Application.Security.InternalApiKey
{
    public sealed class InternalApiKeyResolvedKeyRing
    {
        public InternalApiKeyResolvedKeyRing(
            string currentKeyId,
            string currentApiKey,
            IReadOnlyDictionary<string, string> keys)
        {
            CurrentKeyId = currentKeyId;
            CurrentApiKey = currentApiKey;
            Keys = keys;
        }

        public string CurrentKeyId { get; }
        public string CurrentApiKey { get; }
        public IReadOnlyDictionary<string, string> Keys { get; }
    }
}
