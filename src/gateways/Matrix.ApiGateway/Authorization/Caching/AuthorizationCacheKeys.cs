namespace Matrix.ApiGateway.Authorization.Caching
{
    public static class AuthorizationCacheKeys
    {
        public static string PermissionsVersion(Guid userId)
        {
            return $"pv:{userId:N}";
        }

        public static string PermissionsVersionStale(Guid userId)
        {
            return $"pv:stale:{userId:N}";
        }

        public static string AuthContext(
            Guid userId,
            int permissionsVersion)
        {
            return $"ac:{userId:N}:{permissionsVersion}";
        }

        public static string DefaultUserAccessVersion()
        {
            return "pv:default-user-access";
        }

        public static string DefaultUserAccessVersionStale()
        {
            return "pv:stale:default-user-access";
        }
    }
}
