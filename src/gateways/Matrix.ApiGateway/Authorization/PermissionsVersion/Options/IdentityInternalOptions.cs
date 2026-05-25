using Matrix.BuildingBlocks.Application.Security.InternalApiKey;

namespace Matrix.ApiGateway.Authorization.PermissionsVersion.Options
{
    public sealed class IdentityInternalOptions : IInternalApiKeyRingOptions
    {
        public const string SectionName = "IdentityInternal";

        public string BaseUrl { get; init; } = string.Empty;

        public int RequestTimeoutSeconds { get; init; } = 10;

        public string ApiKey { get; init; } = string.Empty;
        public string? CurrentKeyId { get; init; }
        public IDictionary<string, string>? Keys { get; init; }
    }
}
