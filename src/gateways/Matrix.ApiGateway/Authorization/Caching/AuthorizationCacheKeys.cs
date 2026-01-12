namespace Matrix.ApiGateway.Authorization.Caching
{
    public static class AuthorizationCacheKeys
    {
        public static string PermissionsVersion(Guid userId)
        {
            return $"pv:{userId:N}";
        }

        public static string AuthContext(
            Guid userId,
            int permissionsVersion)
        {
            return $"ac:{userId:N}:{permissionsVersion}";
        }
    }
}
