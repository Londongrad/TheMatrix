namespace Matrix.ApiGateway.Authorization.AuthContext.Options
{
    public sealed class AuthContextOptions
    {
        public const string SectionName = "AuthContext";

        public int CacheTtlSeconds { get; init; } = 1800;
    }
}
