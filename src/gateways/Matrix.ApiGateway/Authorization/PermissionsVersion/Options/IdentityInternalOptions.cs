namespace Matrix.ApiGateway.Authorization.PermissionsVersion.Options
{
    public sealed class IdentityInternalOptions
    {
        public const string SectionName = "IdentityInternal";

        public string BaseUrl { get; init; } = string.Empty;

        public string ApiKey { get; init; } = string.Empty;
    }
}
