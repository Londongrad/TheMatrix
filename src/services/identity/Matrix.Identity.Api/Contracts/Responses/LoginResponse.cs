namespace Matrix.Identity.Api.Contracts.Responses
{
    public sealed class LoginResponse
    {
        public required string AccessToken { get; set; }
        public string TokenType { get; set; } = "Bearer";
        public required int ExpiresIn { get; set; }

        public required string RefreshToken { get; init; }
        public required DateTime RefreshTokenExpiresAtUtc { get; init; }
    }
}