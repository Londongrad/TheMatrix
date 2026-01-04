namespace Matrix.ApiGateway.Authorization.PermissionsVersion.Options
{
    public sealed class PermissionsVersionOptions
    {
        public const string SectionName = "PermissionsVersion";

        public int CacheTtlSeconds { get; init; } = 300;
    }
}
