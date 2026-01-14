using Matrix.BuildingBlocks.Application.Authorization.Jwt;

namespace Matrix.Identity.Infrastructure.Authentication.ExternalJwt
{
    public sealed class ExternalJwtOptions : IJwtValidationOptions
    {
        public const string SectionName = "ExternalJwt";

        public int AccessTokenLifetimeMinutes { get; init; } = 30;

        public int RefreshTokenLifetimeDays { get; init; } = 7;

        public int ShortRefreshTokenLifetimeHours { get; init; } = 8;
        public required string Issuer { get; init; }

        public required string Audience { get; init; }

        public required string SigningKey { get; init; }
    }
}
