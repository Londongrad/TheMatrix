using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityDistricts;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.GetCityResidentialBuildings;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
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

            Task<IReadOnlyList<Domain.Scenarios.ClassicCity.Topology.District>> districtsTask =
                districtRepository.ListByCityIdAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            Task<IReadOnlyList<Domain.Scenarios.ClassicCity.Topology.ResidentialBuilding>> buildingsTask =
                residentialBuildingRepository.ListByCityIdAsync(
                    cityId: cityId,
                    districtId: null,
                    cancellationToken: cancellationToken);
            Task<IReadOnlyList<Domain.Scenarios.ClassicCity.Topology.CityAnchor>> anchorsTask =
                cityAnchorRepository.ListByCityIdAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            Task<IReadOnlyList<Domain.Scenarios.ClassicCity.Topology.RoadNode>> roadNodesTask =
                roadNodeRepository.ListByCityIdAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
            Task<IReadOnlyList<Domain.Scenarios.ClassicCity.Topology.RoadSegment>> roadSegmentsTask =
                roadSegmentRepository.ListByCityIdAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

            await Task.WhenAll(
                districtsTask,
                buildingsTask,
                anchorsTask,
                roadNodesTask,
                roadSegmentsTask);

            return new CityMapTopologyDto(
                CityId: request.CityId,
                Districts: districtsTask.Result
                   .Select(DistrictDto.FromDomain)
                   .ToArray(),
                ResidentialBuildings: buildingsTask.Result
                   .Select(ResidentialBuildingDto.FromDomain)
                   .ToArray(),
                Anchors: anchorsTask.Result
                   .Select(GetCityAnchors.CityAnchorDto.FromDomain)
                   .ToArray(),
                RoadNodes: roadNodesTask.Result
                   .Select(RoadNodeDto.FromDomain)
                   .ToArray(),
                RoadSegments: roadSegmentsTask.Result
                   .Select(RoadSegmentDto.FromDomain)
                   .ToArray());
        }
    }
}
