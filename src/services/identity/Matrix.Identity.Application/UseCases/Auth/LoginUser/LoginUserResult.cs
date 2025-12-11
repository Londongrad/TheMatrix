namespace Matrix.Identity.Application.UseCases.Auth.LoginUser
{
    public sealed class LoginUserResult
    {
        public required string AccessToken { get; init; }
        public required string TokenType { get; init; }
        public required int AccessTokenExpiresInSeconds { get; init; }

        public required string RefreshToken { get; init; }
        public required DateTime RefreshTokenExpiresAtUtc { get; init; }

        public bool IsPersistent { get; set; }
    }
}
