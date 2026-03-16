namespace Matrix.ApiGateway.DownstreamClients.Economy.Models
{
    public sealed record InitializeCityEconomyRequestDto(
        string SimulationKind,
        string? EconomyProfile,
        DateTimeOffset CreatedAtUtc);
}
