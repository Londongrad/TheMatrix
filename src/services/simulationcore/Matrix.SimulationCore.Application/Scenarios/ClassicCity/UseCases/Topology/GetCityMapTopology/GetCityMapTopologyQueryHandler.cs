using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityAnchors;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityMapTopology
{
    public sealed class GetCityMapTopologyQueryHandler(
        IDistrictRepository districtRepository,
        IResidentialBuildingRepository residentialBuildingRepository,
        ICityAnchorRepository cityAnchorRepository,
        IRoadNodeRepository roadNodeRepository,
        IRoadSegmentRepository roadSegmentRepository) : IRequestHandler<GetCityMapTopologyQuery, CityMapTopologyDto>
    {
        public async Task<CityMapTopologyDto> Handle(
            GetCityMapTopologyQuery request,
            CancellationToken cancellationToken)
        {
            var cityId = new CityId(request.CityId);

            // These repositories share the same scoped DbContext, so EF queries must
            // stay sequential inside a single request to avoid concurrency detector failures.
            IReadOnlyList<District> districts =
                await districtRepository.ListByCityIdAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            IReadOnlyList<ResidentialBuilding> buildings =
                await residentialBuildingRepository.ListByCityIdAsync(
                    cityId: cityId,
                    districtId: null,
                    cancellationToken: cancellationToken);
            IReadOnlyList<CityAnchor> anchors =
                await cityAnchorRepository.ListByCityIdAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            IReadOnlyList<RoadNode> roadNodes =
                await roadNodeRepository.ListByCityIdAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            IReadOnlyList<RoadSegment> roadSegments =
                await roadSegmentRepository.ListByCityIdAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            return new CityMapTopologyDto(
                CityId: request.CityId,
                Districts: districts
                   .Select(DistrictDto.FromDomain)
                   .ToArray(),
                ResidentialBuildings: buildings
                   .Select(ResidentialBuildingDto.FromDomain)
                   .ToArray(),
                Anchors: anchors
                   .Select(CityAnchorDto.FromDomain)
                   .ToArray(),
                RoadNodes: roadNodes
                   .Select(RoadNodeDto.FromDomain)
                   .ToArray(),
                RoadSegments: roadSegments
                   .Select(RoadSegmentDto.FromDomain)
                   .ToArray());
        }
    }
}
