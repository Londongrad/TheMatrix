using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityRoadGraph
{
    public sealed class GetCityRoadGraphQueryHandler(
        IDistrictRepository districtRepository,
        IRoadSegmentRepository roadSegmentRepository) : IRequestHandler<GetCityRoadGraphQuery, CityRoadGraphDto>
    {
        public async Task<CityRoadGraphDto> Handle(
            GetCityRoadGraphQuery request,
            CancellationToken cancellationToken)
        {
            var cityId = new CityId(request.CityId);

            IReadOnlyList<District> districts =
                await districtRepository.ListByCityIdAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            IReadOnlyList<RoadSegment> roadSegments =
                await roadSegmentRepository.ListByCityIdAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            return new CityRoadGraphDto(
                CityId: request.CityId,
                Districts: districts
                   .Select(DistrictDto.FromDomain)
                   .ToArray(),
                RoadSegments: roadSegments
                   .Select(RoadSegmentDto.FromDomain)
                   .ToArray());
        }
    }
}
