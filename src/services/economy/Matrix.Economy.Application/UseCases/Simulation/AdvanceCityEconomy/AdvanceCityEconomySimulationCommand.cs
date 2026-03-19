using MediatR;

namespace Matrix.Economy.Application.UseCases.Simulation.AdvanceCityEconomy
{
    public sealed record AdvanceCityEconomySimulationCommand(
        Guid CityId,
        DateTimeOffset FromSimTimeUtc,
        DateTimeOffset ToSimTimeUtc,
        long TickId) : IRequest<AdvanceCityEconomySimulationResult>;
}
