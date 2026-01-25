using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceTime
{
    public sealed record AdvanceCitySimulationCommand(
        Guid CityId,
        TimeSpan RealDelta) : IRequest<bool>;
}
