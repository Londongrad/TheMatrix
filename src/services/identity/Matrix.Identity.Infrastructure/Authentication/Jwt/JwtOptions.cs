namespace Matrix.Identity.Infrastructure.Authentication.Jwt
{
    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = null!;

        public string Audience { get; set; } = null!;

        public string SigningKey { get; set; } = null!;

        public int AccessTokenLifetimeMinutes { get; set; }

        public int RefreshTokenLifetimeDays { get; set; }
    }
}
