using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceTime
{
    public sealed record AdvanceCitySimulationCommand(
        Guid SimulationId,
        TimeSpan RealDelta) : IRequest<bool>;
}
