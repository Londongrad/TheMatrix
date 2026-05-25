using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute
{
    internal static class CityRoutePointResolver
    {
        public static CityRoutePointDto? ResolvePoint(
            string kind,
            Guid entityId,
            IReadOnlyDictionary<Guid, RoadNode> roadNodeById,
            ResidentialBuilding? building,
            CityAnchor? anchor)
        {
            if (string.Equals(
                    a: kind,
                    b: CityRouteMapPointKinds.RoadNode,
                    comparisonType: StringComparison.OrdinalIgnoreCase))
                return roadNodeById.TryGetValue(
                    key: entityId,
                    value: out RoadNode? roadNode)
                    ? new CityRoutePointDto(
                        Kind: CityRouteMapPointKinds.RoadNode,
                        EntityId: roadNode.Id.Value,
                        DistrictId: roadNode.DistrictId.Value,
                        RoadNodeId: roadNode.Id.Value,
                        Name: roadNode.Name,
                        PositionX: roadNode.PositionX,
                        PositionY: roadNode.PositionY)
                    : null;

            if (string.Equals(
                    a: kind,
                    b: CityRouteMapPointKinds.ResidentialBuilding,
                    comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                if (building is null ||
                    building.Id.Value != entityId ||
                    !roadNodeById.ContainsKey(building.AccessRoadNodeId.Value))
                    return null;

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
                if (anchor is null ||
                    anchor.Id.Value != entityId ||
                    !roadNodeById.ContainsKey(anchor.AccessRoadNodeId.Value))
                    return null;

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
