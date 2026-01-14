namespace Matrix.BuildingBlocks.Application.Authorization.Jwt
{
    public interface IJwtValidationOptions
    {
        string Issuer { get; init; }
        string Audience { get; init; }
        string SigningKey { get; init; }
    }
}
