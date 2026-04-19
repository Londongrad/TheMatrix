namespace Matrix.BuildingBlocks.Application.Authorization.Jwt
{
    public interface IInternalJwtKeyRingOptions : IJwtValidationOptions
    {
        int LifetimeSeconds { get; init; }
        string? CurrentKeyId { get; init; }
        IDictionary<string, string>? Keys { get; init; }
    }
}
