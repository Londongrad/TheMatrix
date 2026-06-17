namespace Matrix.Economy.Contracts.Scenarios.ClassicCity.Budget.Requests
{
    public sealed record InitializeCityEconomyRequest(
        string ScenarioKey,
        string? EconomyProfile,
        DateTimeOffset CreatedAtUtc);
}
