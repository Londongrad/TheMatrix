namespace Matrix.ApiGateway.Authorization.InternalJwt
{
    public sealed class InternalJwtOptions
    {
        public const string SectionName = "InternalJwt";

        public string Issuer { get; init; } = null!;
        public string Audience { get; init; } = null!;
        public string SigningKey { get; init; } = null!;

        // Обычно 30–180 секунд хватает (внутрисервисный токен)
        public int LifetimeSeconds { get; init; } = 60;
    }
}
