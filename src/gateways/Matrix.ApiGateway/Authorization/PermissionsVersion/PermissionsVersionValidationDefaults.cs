namespace Matrix.ApiGateway.Authorization.PermissionsVersion
{
    public static class PermissionsVersionValidationDefaults
    {
        public const string StaleTokenItemKey = "PermissionsVersion.TokenStale";
        public const string TokenStaleErrorCode = "Auth.TokenStale";
        public const string TokenStaleMessage = "Access token is stale. Refresh required.";
    }
}
