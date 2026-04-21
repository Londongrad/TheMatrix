using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute
{
    public sealed class ResolveCityRouteQueryHandler(
        IRoadNodeRepository roadNodeRepository,
        IRoadSegmentRepository roadSegmentRepository,
        IResidentialBuildingRepository residentialBuildingRepository,
        ICityAnchorRepository cityAnchorRepository,
        ICityRoadSegmentConditionsClient roadSegmentConditionsClient,
        IClassicCityRoutePlanner routePlanner) : IRequestHandler<ResolveCityRouteQuery, CityRouteDto?>
    {
        public async Task<CityRouteDto?> Handle(
            ResolveCityRouteQuery request,
            CancellationToken cancellationToken)
        {
            var cityId = new CityId(request.CityId);

            Task<IReadOnlyList<RoadNode>> roadNodesTask = roadNodeRepository.ListByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            Task<IReadOnlyList<RoadSegment>> roadSegmentsTask = roadSegmentRepository.ListByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            Task<IReadOnlyList<ResidentialBuilding>> buildingsTask = residentialBuildingRepository.ListByCityIdAsync(
                cityId: cityId,
                districtId: null,
                cancellationToken: cancellationToken);
            Task<IReadOnlyList<CityAnchor>> anchorsTask = cityAnchorRepository.ListByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            Task<Services.Routing.CityRoadSegmentConditionsSnapshot?> conditionsTask =
                roadSegmentConditionsClient.GetByCityIdAsync(
                    cityId: request.CityId,
                    cancellationToken: cancellationToken);

            await Task.WhenAll(
                roadNodesTask,
                roadSegmentsTask,
                buildingsTask,
                anchorsTask,
                conditionsTask);

            IReadOnlyList<RoadNode> roadNodes = await roadNodesTask;
            IReadOnlyList<RoadSegment> roadSegments = await roadSegmentsTask;
            IReadOnlyList<ResidentialBuilding> buildings = await buildingsTask;
            IReadOnlyList<CityAnchor> anchors = await anchorsTask;
            Services.Routing.CityRoadSegmentConditionsSnapshot? conditions = await conditionsTask;

            Dictionary<Guid, RoadNode> roadNodeById = roadNodes.ToDictionary(
                keySelector: x => x.Id.Value,
                elementSelector: x => x);
            Dictionary<Guid, ResidentialBuilding> buildingById = buildings.ToDictionary(
                keySelector: x => x.Id.Value,
                elementSelector: x => x);
            Dictionary<Guid, CityAnchor> anchorById = anchors.ToDictionary(
                keySelector: x => x.Id.Value,
                elementSelector: x => x);

            CityRoutePointDto? from = ResolvePoint(
                kind: CityRouteMapPointKinds.Normalize(request.FromKind),
                entityId: request.FromId,
                roadNodeById: roadNodeById,
                buildingById: buildingById,
                anchorById: anchorById);
            CityRoutePointDto? to = ResolvePoint(
                kind: CityRouteMapPointKinds.Normalize(request.ToKind),
                entityId: request.ToId,
                roadNodeById: roadNodeById,
                buildingById: buildingById,
                anchorById: anchorById);

            if (from is null || to is null)
                return null;

            return routePlanner.Plan(
                cityId: request.CityId,
                profile: CityRouteProfiles.Normalize(request.Profile),
                from: from,
                to: to,
                roadNodes: roadNodes,
                roadSegments: roadSegments,
                segmentConditions: conditions);
        }

        private static CityRoutePointDto? ResolvePoint(
            string kind,
            Guid entityId,
            IReadOnlyDictionary<Guid, RoadNode> roadNodeById,
            IReadOnlyDictionary<Guid, ResidentialBuilding> buildingById,
            IReadOnlyDictionary<Guid, CityAnchor> anchorById)
        {
            if (string.Equals(
                a: kind,
                b: CityRouteMapPointKinds.RoadNode,
                comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                return roadNodeById.TryGetValue(entityId, out RoadNode? roadNode)
                    ? new CityRoutePointDto(
                        Kind: CityRouteMapPointKinds.RoadNode,
                        EntityId: roadNode.Id.Value,
                        DistrictId: roadNode.DistrictId.Value,
                        RoadNodeId: roadNode.Id.Value,
                        Name: roadNode.Name,
                        PositionX: roadNode.PositionX,
                        PositionY: roadNode.PositionY)
                    : null;
            }

            if (string.Equals(
                a: kind,
                b: CityRouteMapPointKinds.ResidentialBuilding,
                comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                if (!buildingById.TryGetValue(entityId, out ResidentialBuilding? building)
                 || !roadNodeById.ContainsKey(building.AccessRoadNodeId.Value))
                {
                    return null;
                }

                return new CityRoutePointDto(
                    Kind: CityRouteMapPointKinds.ResidentialBuilding,
                    EntityId: building.Id.Value,
                    DistrictId: building.DistrictId.Value,
                    RoadNodeId: building.AccessRoadNodeId.Value,
                    Name: building.Name.Value,
                    PositionX: building.PositionX,
                    PositionY: building.PositionY);
            }

            if (string.Equals(
                a: kind,
                b: CityRouteMapPointKinds.CityAnchor,
                comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                if (!anchorById.TryGetValue(entityId, out CityAnchor? anchor)
                 || !roadNodeById.ContainsKey(anchor.AccessRoadNodeId.Value))
                {
                    return null;
                }

                return new CityRoutePointDto(
                    Kind: CityRouteMapPointKinds.CityAnchor,
                    EntityId: anchor.Id.Value,
                    DistrictId: anchor.DistrictId.Value,
                    RoadNodeId: anchor.AccessRoadNodeId.Value,
                    Name: anchor.Name.Value,
                    PositionX: anchor.PositionX,
                    PositionY: anchor.PositionY);
            }

            return null;
        }
    }
}
