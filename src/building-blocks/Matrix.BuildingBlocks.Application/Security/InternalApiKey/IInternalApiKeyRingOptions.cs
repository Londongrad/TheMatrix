namespace Matrix.BuildingBlocks.Application.Security.InternalApiKey
{
    public interface IInternalApiKeyRingOptions
    {
        string ApiKey { get; init; }
        string? CurrentKeyId { get; init; }
        IDictionary<string, string>? Keys { get; init; }
    }
}
