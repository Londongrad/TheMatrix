namespace Matrix.ApiGateway.Authorization.Caching
{
    public static class AuthorizationCacheKeys
    {
        public static string PermissionsVersion(Guid userId) => $"pv:{userId:N}";

        public static string AuthContext(Guid userId, int permissionsVersion)
            => $"ac:{userId:N}:{permissionsVersion}";
    }
}
