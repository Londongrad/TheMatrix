namespace Matrix.Identity.Contracts.Auth.Responses
{
    public sealed class LoginResponse
    {
        public required string AccessToken { get; init; }
        public string TokenType { get; init; } = "Bearer";
        public required int ExpiresIn { get; init; }

        public required string RefreshToken { get; set; }
        public required DateTime RefreshTokenExpiresAtUtc { get; init; }

        public bool IsPersistent { get; init; }
    }
}
