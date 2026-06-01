using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityAnchors
{
    public sealed class GetCityAnchorsQueryHandler(ICityAnchorRepository repository)
        : IRequestHandler<GetCityAnchorsQuery, IReadOnlyList<CityAnchorDto>>
    {
        public async Task<IReadOnlyList<CityAnchorDto>> Handle(
            GetCityAnchorsQuery request,
            CancellationToken cancellationToken)
        {
            return (await repository.ListByCityIdAsync(
                    cityId: new CityId(request.CityId),
                    cancellationToken: cancellationToken))
               .Select(CityAnchorDto.FromDomain)
               .ToArray();
        }
    }
}
