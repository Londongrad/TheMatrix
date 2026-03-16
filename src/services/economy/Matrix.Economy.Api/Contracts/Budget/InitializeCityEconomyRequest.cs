namespace Matrix.Economy.Api.Contracts.Budget
{
    public sealed record InitializeCityEconomyRequest(
        string SimulationKind,
        string? EconomyProfile,
        DateTimeOffset CreatedAtUtc);
}
