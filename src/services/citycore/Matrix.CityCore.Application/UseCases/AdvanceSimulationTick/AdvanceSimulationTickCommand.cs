using MediatR;

namespace Matrix.CityCore.Application.UseCases.AdvanceSimulationTick
{
    public sealed record AdvanceSimulationTickCommand : IRequest<Unit>;
}
