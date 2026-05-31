namespace Matrix.Economy.Contracts.Budget.Requests
{
    public sealed record InitializeCityEconomyRequest(
        string ScenarioKey,
        string? EconomyProfile,
        DateTimeOffset CreatedAtUtc);
}
