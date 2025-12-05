namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts
{
    public sealed class LoginResponse
    {
        public string AccessToken { get; set; } = null!;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }

        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiresAtUtc { get; init; }
    }
}