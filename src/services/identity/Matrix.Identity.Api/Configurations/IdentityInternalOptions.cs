using Matrix.BuildingBlocks.Application.Security.InternalApiKey;

namespace Matrix.Identity.Api.Configurations
{
    public sealed class IdentityInternalOptions : IInternalApiKeyRingOptions
    {
        public const string SectionName = "IdentityInternal";

        public string ApiKey { get; init; } = string.Empty;
        public string? CurrentKeyId { get; init; }
        public IDictionary<string, string>? Keys { get; init; }
    }
}
