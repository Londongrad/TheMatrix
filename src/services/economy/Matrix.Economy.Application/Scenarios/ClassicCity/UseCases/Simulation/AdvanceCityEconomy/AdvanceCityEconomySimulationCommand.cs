using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Simulation.AdvanceCityEconomy
{
    public sealed record AdvanceCityEconomySimulationCommand(
        Guid CityId,
        DateTimeOffset FromSimTimeUtc,
        DateTimeOffset ToSimTimeUtc,
        long TickId) : IRequest<AdvanceCityEconomySimulationResult>;
}
