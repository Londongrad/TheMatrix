using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoutesBatch
{
    public sealed class ResolveCityRoutesBatchQueryHandler(
        IRoadNodeRepository roadNodeRepository,
        IRoadSegmentRepository roadSegmentRepository,
        IResidentialBuildingRepository residentialBuildingRepository,
        ICityAnchorRepository cityAnchorRepository,
        ICityRoadSegmentConditionsClient roadSegmentConditionsClient,
        IClassicCityRoutePlanner routePlanner)
        : IRequestHandler<ResolveCityRoutesBatchQuery, ResolveCityRoutesBatchResult>
    {
        public async Task<ResolveCityRoutesBatchResult> Handle(
            ResolveCityRoutesBatchQuery request,
            CancellationToken cancellationToken)
        {
            var cityId = new CityId(request.CityId);

            Task<CityRoadSegmentConditionsSnapshot?> conditionsTask =
                roadSegmentConditionsClient.GetByCityIdAsync(
                    cityId: request.CityId,
                    cancellationToken: cancellationToken);

            // Repositories share one scoped DbContext, so keep EF reads sequential.
            // The external conditions request can run in parallel safely.
            IReadOnlyList<RoadNode> roadNodes = await roadNodeRepository.ListByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            IReadOnlyList<RoadSegment> roadSegments = await roadSegmentRepository.ListByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            IReadOnlyList<ResidentialBuilding> residentialBuildings =
                await residentialBuildingRepository.ListByCityIdAsync(
                    cityId: cityId,
                    districtId: null,
                    cancellationToken: cancellationToken);
            IReadOnlyList<CityAnchor> cityAnchors = await cityAnchorRepository.ListByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            CityRoadSegmentConditionsSnapshot? conditions = await conditionsTask;

            var roadNodeById = roadNodes.ToDictionary(
                keySelector: x => x.Id.Value,
                elementSelector: x => x);
            var residentialBuildingById = residentialBuildings.ToDictionary(
                keySelector: x => x.Id.Value,
                elementSelector: x => x);
            var cityAnchorById = cityAnchors.ToDictionary(
                keySelector: x => x.Id.Value,
                elementSelector: x => x);
            var plannedRoutesByKey = new Dictionary<RouteDeduplicationKey, CityRouteDto?>();
            List<ResolvedCityRouteBatchItemDto> results = new(capacity: request.Routes.Count);

            foreach (ResolveCityRoutesBatchQueryItem item in request.Routes)
            {
                string fromKind = CityRouteMapPointKinds.Normalize(item.FromKind);
                string toKind = CityRouteMapPointKinds.Normalize(item.ToKind);
                string profile = CityRouteProfiles.Normalize(item.Profile);
                var key = new RouteDeduplicationKey(
                    FromKind: fromKind,
                    FromId: item.FromId,
                    ToKind: toKind,
                    ToId: item.ToId,
                    Profile: profile);

                if (!plannedRoutesByKey.TryGetValue(
                        key: key,
                        value: out CityRouteDto? route))
                {
                    route = ResolveRoute(
                        cityId: request.CityId,
                        fromKind: fromKind,
                        fromId: item.FromId,
                        toKind: toKind,
                        toId: item.ToId,
                        profile: profile,
                        roadNodes: roadNodes,
                        roadSegments: roadSegments,
                        roadNodeById: roadNodeById,
                        residentialBuildingById: residentialBuildingById,
                        cityAnchorById: cityAnchorById,
                        conditions: conditions);
                    plannedRoutesByKey[key] = route;
                }

                results.Add(
                    new ResolvedCityRouteBatchItemDto(
                        Index: item.Index,
                        Route: route));
            }

            return new ResolveCityRoutesBatchResult(results);
        }

        private CityRouteDto? ResolveRoute(
            Guid cityId,
            string fromKind,
            Guid fromId,
            string toKind,
            Guid toId,
            string profile,
            IReadOnlyList<RoadNode> roadNodes,
            IReadOnlyList<RoadSegment> roadSegments,
            IReadOnlyDictionary<Guid, RoadNode> roadNodeById,
            IReadOnlyDictionary<Guid, ResidentialBuilding> residentialBuildingById,
            IReadOnlyDictionary<Guid, CityAnchor> cityAnchorById,
            CityRoadSegmentConditionsSnapshot? conditions)
        {
            CityRoutePointDto? from = ResolvePoint(
                kind: fromKind,
                entityId: fromId,
                roadNodeById: roadNodeById,
                residentialBuildingById: residentialBuildingById,
                cityAnchorById: cityAnchorById);
            CityRoutePointDto? to = ResolvePoint(
                kind: toKind,
                entityId: toId,
                roadNodeById: roadNodeById,
                residentialBuildingById: residentialBuildingById,
                cityAnchorById: cityAnchorById);

            if (from is null || to is null)
                return null;

            return routePlanner.Plan(
                cityId: cityId,
                profile: profile,
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
            IReadOnlyDictionary<Guid, ResidentialBuilding> residentialBuildingById,
            IReadOnlyDictionary<Guid, CityAnchor> cityAnchorById)
        {
            ResidentialBuilding? building =
                string.Equals(
                    a: kind,
                    b: CityRouteMapPointKinds.ResidentialBuilding,
                    comparisonType: StringComparison.OrdinalIgnoreCase) &&
                residentialBuildingById.TryGetValue(
                    key: entityId,
                    value: out ResidentialBuilding? resolvedBuilding)
                    ? resolvedBuilding
                    : null;
            CityAnchor? anchor =
                string.Equals(
                    a: kind,
                    b: CityRouteMapPointKinds.CityAnchor,
                    comparisonType: StringComparison.OrdinalIgnoreCase) &&
                cityAnchorById.TryGetValue(
                    key: entityId,
                    value: out CityAnchor? resolvedAnchor)
                    ? resolvedAnchor
                    : null;

            return CityRoutePointResolver.ResolvePoint(
                kind: kind,
                entityId: entityId,
                roadNodeById: roadNodeById,
                building: building,
                anchor: anchor);
        }

        private readonly record struct RouteDeduplicationKey(
            string FromKind,
            Guid FromId,
            string ToKind,
            Guid ToId,
            string Profile);
    }
}
