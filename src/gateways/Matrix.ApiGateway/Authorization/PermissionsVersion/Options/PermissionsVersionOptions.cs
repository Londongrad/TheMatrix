namespace Matrix.ApiGateway.Authorization.PermissionsVersion.Options
{
    public sealed class PermissionsVersionOptions
    {
        public const string SectionName = "PermissionsVersion";

        public int CacheTtlSeconds { get; init; } = 300;

        public int StaleCacheTtlSeconds { get; init; } = 21600;

        public bool AllowStaleCacheOnIdentityFailure { get; init; } = true;
    }
}
