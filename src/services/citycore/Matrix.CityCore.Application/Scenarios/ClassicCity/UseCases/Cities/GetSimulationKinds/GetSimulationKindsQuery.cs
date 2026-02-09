using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSimulationKinds
{
    public sealed record GetSimulationKindsQuery : IRequest<IReadOnlyList<SimulationKindCatalogItemDto>>;
}
