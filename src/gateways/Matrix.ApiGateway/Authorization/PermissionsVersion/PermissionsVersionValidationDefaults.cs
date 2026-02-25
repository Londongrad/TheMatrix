namespace Matrix.ApiGateway.Authorization.PermissionsVersion
{
    public static class PermissionsVersionValidationDefaults
    {
        public const string StaleTokenItemKey = "PermissionsVersion.TokenStale";
        public const string UnavailableItemKey = "PermissionsVersion.Unavailable";
        public const string TokenStaleErrorCode = "Auth.TokenStale";
        public const string TokenStaleMessage = "Access token is stale. Refresh required.";
        public const string UnavailableErrorCode = "Auth.DependencyUnavailable";
        public const string UnavailableMessage = "Authentication dependency is temporarily unavailable. Please retry.";
    }
}
