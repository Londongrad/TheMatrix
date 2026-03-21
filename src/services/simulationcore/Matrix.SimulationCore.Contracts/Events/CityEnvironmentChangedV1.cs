namespace Matrix.SimulationCore.Contracts.Events
{
    public sealed record CityEnvironmentChangedV1(
        Guid CityId,
        CityEnvironmentV1? PreviousEnvironment,
        CityEnvironmentV1 CurrentEnvironment,
        DateTimeOffset OccurredOnUtc);
}
