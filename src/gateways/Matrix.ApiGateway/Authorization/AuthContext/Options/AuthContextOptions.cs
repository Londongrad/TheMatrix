namespace Matrix.ApiGateway.Authorization.AuthContext.Options
{
    public sealed class AuthContextOptions
    {
        public int CacheTtlSeconds { get; init; } = 1800;
    }
}
