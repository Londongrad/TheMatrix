namespace Matrix.Identity.Api.Configurations
{
    public sealed class IdentityInternalOptions
    {
        public const string SectionName = "IdentityInternal";

        public string ApiKey { get; init; } = string.Empty;
    }
}
