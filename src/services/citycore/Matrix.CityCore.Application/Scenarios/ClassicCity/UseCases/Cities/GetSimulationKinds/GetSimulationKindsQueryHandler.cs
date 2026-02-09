using Matrix.CityCore.Application.Services.Bootstrap.Abstractions;
using MediatR;

namespace Matrix.CityCore.Application.Scenarios.ClassicCity.UseCases.Cities.GetSimulationKinds
{
    public sealed class GetSimulationKindsQueryHandler(
        IEnumerable<ICitySimulationBootstrapStrategy> simulationBootstrapStrategies)
        : IRequestHandler<GetSimulationKindsQuery, IReadOnlyList<SimulationKindCatalogItemDto>>
    {
        public Task<IReadOnlyList<SimulationKindCatalogItemDto>> Handle(
            GetSimulationKindsQuery request,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<SimulationKindCatalogItemDto> items = simulationBootstrapStrategies
               .Select(strategy => SimulationKindCatalogItemDto.FromDescriptor(strategy.Descriptor))
               .DistinctBy(item => item.Kind)
               .OrderByDescending(item => item.IsDefault)
               .ThenBy(
                    keySelector: item => item.DisplayName,
                    comparer: StringComparer.Ordinal)
               .ToArray();

            return Task.FromResult(items);
        }
    }
}
