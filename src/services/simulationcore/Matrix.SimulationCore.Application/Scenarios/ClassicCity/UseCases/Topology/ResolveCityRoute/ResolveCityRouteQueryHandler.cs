using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing;
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

            Task<CityRoadSegmentConditionsSnapshot?> conditionsTask =
                roadSegmentConditionsClient.GetByCityIdAsync(
                    cityId: request.CityId,
                    cancellationToken: cancellationToken);

            // The repositories below share one scoped DbContext, so keep the EF reads
            // sequential. The downstream HTTP call can still run in parallel safely.
            IReadOnlyList<RoadNode> roadNodes = await roadNodeRepository.ListByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            IReadOnlyList<RoadSegment> roadSegments = await roadSegmentRepository.ListByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            CityRoadSegmentConditionsSnapshot? conditions = await conditionsTask;

            var roadNodeById = roadNodes.ToDictionary(
                keySelector: x => x.Id.Value,
                elementSelector: x => x);
            ResidentialBuilding? sourceBuilding = string.Equals(
                a: CityRouteMapPointKinds.Normalize(request.FromKind),
                b: CityRouteMapPointKinds.ResidentialBuilding,
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? await residentialBuildingRepository.GetByIdAsync(
                    buildingId: new ResidentialBuildingId(request.FromId),
                    cancellationToken: cancellationToken)
                : null;
            ResidentialBuilding? targetBuilding = string.Equals(
                a: CityRouteMapPointKinds.Normalize(request.ToKind),
                b: CityRouteMapPointKinds.ResidentialBuilding,
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? await residentialBuildingRepository.GetByIdAsync(
                    buildingId: new ResidentialBuildingId(request.ToId),
                    cancellationToken: cancellationToken)
                : null;
            CityAnchor? sourceAnchor = string.Equals(
                a: CityRouteMapPointKinds.Normalize(request.FromKind),
                b: CityRouteMapPointKinds.CityAnchor,
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? await cityAnchorRepository.GetByIdAsync(
                    anchorId: new CityAnchorId(request.FromId),
                    cancellationToken: cancellationToken)
                : null;
            CityAnchor? targetAnchor = string.Equals(
                a: CityRouteMapPointKinds.Normalize(request.ToKind),
                b: CityRouteMapPointKinds.CityAnchor,
                comparisonType: StringComparison.OrdinalIgnoreCase)
                ? await cityAnchorRepository.GetByIdAsync(
                    anchorId: new CityAnchorId(request.ToId),
                    cancellationToken: cancellationToken)
                : null;

            CityRoutePointDto? from = ResolvePoint(
                kind: CityRouteMapPointKinds.Normalize(request.FromKind),
                entityId: request.FromId,
                roadNodeById: roadNodeById,
                building: sourceBuilding,
                anchor: sourceAnchor);
            CityRoutePointDto? to = ResolvePoint(
                kind: CityRouteMapPointKinds.Normalize(request.ToKind),
                entityId: request.ToId,
                roadNodeById: roadNodeById,
                building: targetBuilding,
                anchor: targetAnchor);

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
            ResidentialBuilding? building,
            CityAnchor? anchor)
        {
            return CityRoutePointResolver.ResolvePoint(
                kind: kind,
                entityId: entityId,
                roadNodeById: roadNodeById,
                building: building,
                anchor: anchor);
        }
    }
}
