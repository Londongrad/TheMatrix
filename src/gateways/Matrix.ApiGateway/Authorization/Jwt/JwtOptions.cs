namespace Matrix.ApiGateway.Authorization.Jwt
{
    public sealed class JwtOptions
    {
        public string Issuer { get; init; } = null!;
        public string Audience { get; init; } = null!;
        public string SigningKey { get; init; } = null!;
    }
}
