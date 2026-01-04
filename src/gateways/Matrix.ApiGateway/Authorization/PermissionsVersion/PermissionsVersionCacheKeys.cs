namespace Matrix.ApiGateway.Authorization.PermissionsVersion
{
    public static class PermissionsVersionCacheKeys
    {
        public static string ForUser(Guid userId) => $"pv:{userId:D}";
    }
}
