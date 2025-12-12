namespace Matrix.Identity.Infrastructure.Authentication.Jwt
{
    public sealed class JwtOptions
    {
        public required string Issuer { get; set; }

        public required string Audience { get; set; }

        public required string SigningKey { get; set; }

        public int AccessTokenLifetimeMinutes { get; set; } = 30;

        public int RefreshTokenLifetimeDays { get; set; } = 7;

        public int ShortRefreshTokenLifetimeHours { get; set; } = 8;
    }
}
