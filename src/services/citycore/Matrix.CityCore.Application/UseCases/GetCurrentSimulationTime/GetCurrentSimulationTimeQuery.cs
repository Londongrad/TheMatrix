using MediatR;

namespace Matrix.CityCore.Application.UseCases.GetCurrentSimulationTime
{
    public sealed record GetCurrentSimulationTimeQuery : IRequest<SimulationTimeDto>;
}
