namespace Matrix.BuildingBlocks.Application.Authorization.Jwt
{
    public sealed class InternalJwtResolvedKeyRing
    {
        public InternalJwtResolvedKeyRing(
            string currentKeyId,
            string currentSigningKey,
            IReadOnlyDictionary<string, string> keys)
        {
            CurrentKeyId = currentKeyId;
            CurrentSigningKey = currentSigningKey;
            Keys = keys;
        }

        public string CurrentKeyId { get; }
        public string CurrentSigningKey { get; }
        public IReadOnlyDictionary<string, string> Keys { get; }
    }
}
