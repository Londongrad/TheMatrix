namespace Matrix.Economy.Contracts.Budget.Requests
{
    public sealed record InitializeCityEconomyRequest(
        string SimulationKind,
        string? EconomyProfile,
        DateTimeOffset CreatedAtUtc);
}
