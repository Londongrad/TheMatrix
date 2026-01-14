namespace Matrix.BuildingBlocks.Application.Authorization.Jwt
{
    public sealed class InternalJwtOptions : IJwtValidationOptions
    {
        public const string SectionName = "InternalJwt";

        public required string Issuer { get; init; }
        public required string Audience { get; init; }
        public required string SigningKey { get; init; }

        public int LifetimeSeconds { get; init; } = 60;
    }
}
