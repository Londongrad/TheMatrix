using MediatR;

namespace Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy
{
    public sealed record InitializeCityEconomyCommand(
        Guid CityId,
        string SimulationKind,
        string? EconomyProfile,
        DateTimeOffset CreatedAtUtc) : IRequest<CityEconomyBootstrapResultDto>;
}
