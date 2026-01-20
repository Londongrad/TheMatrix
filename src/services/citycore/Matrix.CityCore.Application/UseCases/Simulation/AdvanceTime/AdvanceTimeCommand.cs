using MediatR;

namespace Matrix.CityCore.Application.UseCases.Simulation.AdvanceTime
{
    public sealed record AdvanceTimeCommand(
        Guid CityId,
        TimeSpan RealDelta) : IRequest<bool>;
}
