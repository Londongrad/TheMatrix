namespace Matrix.ApiGateway.Authorization.ExternalJwt
{
    public sealed class ExternalJwtOptions
    {
        public string Issuer { get; init; } = null!;
        public string Audience { get; init; } = null!;
        public string SigningKey { get; init; } = null!;
    }
}
