namespace Matrix.BuildingBlocks.Application.Authorization.Jwt
{
    public sealed class InternalUserContextJwtOptions : IInternalJwtKeyRingOptions
    {
        public const string SectionName = "InternalUserContextJwt";

        public int LifetimeSeconds { get; init; } = 60;

        public string? CurrentKeyId { get; init; }
        public IDictionary<string, string>? Keys { get; init; }

        public required string Issuer { get; init; }
        public required string Audience { get; init; }
        public string SigningKey { get; init; } = string.Empty;
    }
}
