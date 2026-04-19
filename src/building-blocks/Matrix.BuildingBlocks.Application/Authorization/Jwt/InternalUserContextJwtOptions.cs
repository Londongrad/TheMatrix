namespace Matrix.BuildingBlocks.Application.Authorization.Jwt
{
    public sealed class InternalUserContextJwtOptions : IJwtValidationOptions
    {
        public const string SectionName = "InternalUserContextJwt";

        public int LifetimeSeconds { get; init; } = 60;

        public required string Issuer { get; init; }
        public required string Audience { get; init; }
        public required string SigningKey { get; init; }
    }
}
