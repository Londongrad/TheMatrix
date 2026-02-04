namespace Matrix.CityCore.Contracts.Events
{
    public sealed record CityEnvironmentChangedV1(
        Guid CityId,
        CityEnvironmentV1? PreviousEnvironment,
        CityEnvironmentV1 CurrentEnvironment,
        DateTimeOffset OccurredOnUtc);
}
