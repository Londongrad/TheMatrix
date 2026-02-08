using MediatR;

namespace Matrix.CityCore.Application.UseCases.Cities.GetSimulationKinds
{
    public sealed record GetSimulationKindsQuery : IRequest<IReadOnlyList<SimulationKindCatalogItemDto>>;
}
