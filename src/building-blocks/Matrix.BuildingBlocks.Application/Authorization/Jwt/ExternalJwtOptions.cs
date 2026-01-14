namespace Matrix.BuildingBlocks.Application.Authorization.Jwt
{
    public sealed class ExternalJwtOptions : IJwtValidationOptions
    {
        public const string SectionName = "ExternalJwt";

        public required string Issuer { get; init; }
        public required string Audience { get; init; }
        public required string SigningKey { get; init; }
    }
}
