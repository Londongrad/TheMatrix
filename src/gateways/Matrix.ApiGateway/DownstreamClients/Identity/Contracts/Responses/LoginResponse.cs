namespace Matrix.ApiGateway.DownstreamClients.Identity.Contracts.Responses
{
    public sealed class LoginResponse
    {
        public required string AccessToken { get; set; }
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }

        public required string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiresAtUtc { get; init; }
    }
}
