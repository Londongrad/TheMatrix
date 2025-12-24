namespace Matrix.Identity.Application.UseCases.Self.Auth
{
    public sealed class AccessTokenModel
    {
        public required string Token { get; init; }

        public string TokenType { get; init; } = "Bearer";

        /// <summary>
        ///     Время жизни токена в секундах.
        /// </summary>
        public required int ExpiresInSeconds { get; init; }
    }
}
