using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing
{
    public sealed class ClassicCityRoutePlanner : IClassicCityRoutePlanner
    {
        private const decimal MinimumTraversablePassability = 0.18m;
        private const decimal MinimumEffectiveSpeedMetersPerMinute = 12m;
        private const decimal MaximumClosureRisk = 0.97m;

        public CityRouteDto Plan(
            Guid cityId,
            string profile,
            CityRoutePointDto from,
            CityRoutePointDto to,
            IReadOnlyList<RoadNode> roadNodes,
            IReadOnlyList<RoadSegment> roadSegments,
            CityRoadSegmentConditionsSnapshot? segmentConditions)
        {
            Dictionary<Guid, RoadNode> roadNodeById = roadNodes.ToDictionary(
                keySelector: x => x.Id.Value,
                elementSelector: x => x);
            Dictionary<Guid, SegmentConditionState> conditionBySegmentId =
                BuildConditionIndex(segmentConditions);

            if (from.RoadNodeId == to.RoadNodeId)
            {
                return new CityRouteDto(
                    CityId: cityId,
                    Profile: profile,
                    Accessible: true,
                    UsedDynamicRoadConditions: segmentConditions is not null,
                    EffectiveTickId: segmentConditions?.EffectiveTickId,
                    ConditionsLastEvaluatedAtUtc: segmentConditions?.LastEvaluatedAtUtc,
                    From: from,
                    To: to,
                    TotalDistanceMeters: 0m,
                    EstimatedTravelTimeMinutes: 0m,
                    OverallPassabilityIndex: 1m,
                    UnreachableReason: null,
                    Segments: Array.Empty<CityRouteSegmentDto>());
            }

            Dictionary<Guid, List<TraversalEdge>> adjacency = BuildAdjacency(
                roadSegments: roadSegments,
                conditionBySegmentId: conditionBySegmentId,
                profile: profile);

            if (!adjacency.ContainsKey(from.RoadNodeId) || !roadNodeById.ContainsKey(to.RoadNodeId))
            {
                return CreateUnreachable(
                    cityId: cityId,
                    profile: profile,
                    from: from,
                    to: to,
                    segmentConditions: segmentConditions,
                    reason: "No traversable route could be built for the selected map points.");
            }

            PriorityQueue<Guid, decimal> frontier = new();
            Dictionary<Guid, decimal> costByRoadNodeId = new() { [from.RoadNodeId] = 0m };
            Dictionary<Guid, PreviousStep> previousByRoadNodeId = new();

            frontier.Enqueue(from.RoadNodeId, 0m);

            while (frontier.Count > 0)
            {
                Guid currentRoadNodeId = frontier.Dequeue();
                decimal currentCost = costByRoadNodeId[currentRoadNodeId];

                if (currentRoadNodeId == to.RoadNodeId)
                    break;

                if (!adjacency.TryGetValue(currentRoadNodeId, out List<TraversalEdge>? edges))
                    continue;

                foreach (TraversalEdge edge in edges)
                {
                    decimal nextCost = currentCost + edge.TraversalMinutes;

                    if (costByRoadNodeId.TryGetValue(edge.ToRoadNodeId, out decimal existingCost)
                     && existingCost <= nextCost)
                    {
                        continue;
                    }

                    costByRoadNodeId[edge.ToRoadNodeId] = nextCost;
                    previousByRoadNodeId[edge.ToRoadNodeId] = new PreviousStep(
                        PreviousRoadNodeId: currentRoadNodeId,
                        Edge: edge);
                    frontier.Enqueue(edge.ToRoadNodeId, nextCost);
                }
            }

            if (!previousByRoadNodeId.ContainsKey(to.RoadNodeId))
            {
                return CreateUnreachable(
                    cityId: cityId,
                    profile: profile,
                    from: from,
                    to: to,
                    segmentConditions: segmentConditions,
                    reason: "No traversable route is currently available between the selected points.");
            }

            List<CityRouteSegmentDto> routeSegments = new();
            Guid traversalRoadNodeId = to.RoadNodeId;
            decimal totalDistanceMeters = 0m;
            decimal totalTraversalMinutes = 0m;
            decimal weightedPassability = 0m;

            while (traversalRoadNodeId != from.RoadNodeId)
            {
                PreviousStep step = previousByRoadNodeId[traversalRoadNodeId];
                TraversalEdge edge = step.Edge;

                routeSegments.Add(new CityRouteSegmentDto(
                    RoadSegmentId: edge.RoadSegment.Id.Value,
                    DistrictId: edge.RoadSegment.DistrictId.Value,
                    FromRoadNodeId: edge.FromRoadNodeId,
                    ToRoadNodeId: edge.ToRoadNodeId,
                    Name: edge.RoadSegment.Name,
                    Type: edge.RoadSegment.Type.ToString(),
                    LengthMeters: edge.RoadSegment.LengthMeters,
                    EstimatedTraversalMinutes: edge.TraversalMinutes,
                    PassabilityIndex: edge.Condition.PassabilityIndex,
                    SpeedMultiplierIndex: edge.Condition.SpeedMultiplierIndex,
                    SlipRiskIndex: edge.Condition.SlipRiskIndex,
                    ClosureRiskIndex: edge.Condition.ClosureRiskIndex));

                totalDistanceMeters += edge.RoadSegment.LengthMeters;
                totalTraversalMinutes += edge.TraversalMinutes;
                weightedPassability += edge.Condition.PassabilityIndex * edge.RoadSegment.LengthMeters;
                traversalRoadNodeId = step.PreviousRoadNodeId;
            }

            routeSegments.Reverse();

            decimal overallPassabilityIndex = totalDistanceMeters <= 0m
                ? 1m
                : decimal.Round(
                    d: weightedPassability / totalDistanceMeters,
                    decimals: 4);

            return new CityRouteDto(
                CityId: cityId,
                Profile: profile,
                Accessible: true,
                UsedDynamicRoadConditions: segmentConditions is not null,
                EffectiveTickId: segmentConditions?.EffectiveTickId,
                ConditionsLastEvaluatedAtUtc: segmentConditions?.LastEvaluatedAtUtc,
                From: from,
                To: to,
                TotalDistanceMeters: decimal.Round(totalDistanceMeters, 2),
                EstimatedTravelTimeMinutes: decimal.Round(totalTraversalMinutes, 2),
                OverallPassabilityIndex: overallPassabilityIndex,
                UnreachableReason: null,
                Segments: routeSegments);
        }

        private static Dictionary<Guid, List<TraversalEdge>> BuildAdjacency(
            IReadOnlyList<RoadSegment> roadSegments,
            IReadOnlyDictionary<Guid, SegmentConditionState> conditionBySegmentId,
            string profile)
        {
            Dictionary<Guid, List<TraversalEdge>> adjacency = new();

            foreach (RoadSegment roadSegment in roadSegments)
            {
                SegmentConditionState condition = conditionBySegmentId.TryGetValue(
                    key: roadSegment.Id.Value,
                    value: out SegmentConditionState? existingCondition)
                    ? existingCondition
                    : SegmentConditionState.Neutral;

                if (!IsTraversable(condition))
                    continue;

                decimal traversalMinutes = CalculateTraversalMinutes(
                    roadSegment: roadSegment,
                    condition: condition,
                    profile: profile);

                AddEdge(
                    adjacency: adjacency,
                    edge: new TraversalEdge(
                        RoadSegment: roadSegment,
                        FromRoadNodeId: roadSegment.FromRoadNodeId.Value,
                        ToRoadNodeId: roadSegment.ToRoadNodeId.Value,
                        TraversalMinutes: traversalMinutes,
                        Condition: condition));
                AddEdge(
                    adjacency: adjacency,
                    edge: new TraversalEdge(
                        RoadSegment: roadSegment,
                        FromRoadNodeId: roadSegment.ToRoadNodeId.Value,
                        ToRoadNodeId: roadSegment.FromRoadNodeId.Value,
                        TraversalMinutes: traversalMinutes,
                        Condition: condition));
            }

            return adjacency;
        }

        private static void AddEdge(
            IDictionary<Guid, List<TraversalEdge>> adjacency,
            TraversalEdge edge)
        {
            if (!adjacency.TryGetValue(edge.FromRoadNodeId, out List<TraversalEdge>? edges))
            {
                edges = new List<TraversalEdge>();
                adjacency[edge.FromRoadNodeId] = edges;
            }

            edges.Add(edge);
        }

        private static Dictionary<Guid, SegmentConditionState> BuildConditionIndex(
            CityRoadSegmentConditionsSnapshot? segmentConditions)
        {
            if (segmentConditions is null)
                return new Dictionary<Guid, SegmentConditionState>();

            return segmentConditions.Segments.ToDictionary(
                keySelector: x => x.RoadSegmentId,
                elementSelector: x => new SegmentConditionState(
                    PassabilityIndex: x.PassabilityIndex,
                    SpeedMultiplierIndex: x.SpeedMultiplierIndex,
                    SlipRiskIndex: x.SlipRiskIndex,
                    ClosureRiskIndex: x.ClosureRiskIndex));
        }

        private static decimal CalculateTraversalMinutes(
            RoadSegment roadSegment,
            SegmentConditionState condition,
            string profile)
        {
            decimal baseSpeedMetersPerMinute = ResolveBaseSpeedMetersPerMinute(profile);
            decimal roadTypeFactor = ResolveRoadTypeFactor(
                profile: profile,
                roadSegmentType: roadSegment.Type);
            decimal passabilityFactor = 0.25m + (condition.PassabilityIndex * 0.75m);
            decimal slipSafetyFactor = 1m - (condition.SlipRiskIndex * 0.18m);
            decimal effectiveSpeedMultiplier =
                condition.SpeedMultiplierIndex * roadTypeFactor * passabilityFactor * slipSafetyFactor;
            decimal effectiveSpeedMetersPerMinute = Math.Max(
                val1: MinimumEffectiveSpeedMetersPerMinute,
                val2: baseSpeedMetersPerMinute * effectiveSpeedMultiplier);

            return roadSegment.LengthMeters / effectiveSpeedMetersPerMinute;
        }

        private static decimal ResolveBaseSpeedMetersPerMinute(string profile)
        {
            return profile switch
            {
                CityRouteProfiles.EmergencyResponse => 520m,
                CityRouteProfiles.ServiceVehicle => 360m,
                _ => 78m
            };
        }

        private static decimal ResolveRoadTypeFactor(
            string profile,
            RoadSegmentType roadSegmentType)
        {
            if (string.Equals(
                a: profile,
                b: CityRouteProfiles.Pedestrian,
                comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                return roadSegmentType switch
                {
                    RoadSegmentType.Arterial => 0.92m,
                    RoadSegmentType.Collector => 1m,
                    RoadSegmentType.LocalAccess => 1.04m,
                    _ => 1m
                };
            }

            return roadSegmentType switch
            {
                RoadSegmentType.Arterial => 1.08m,
                RoadSegmentType.Collector => 1m,
                RoadSegmentType.LocalAccess => 0.82m,
                _ => 1m
            };
        }

        private static bool IsTraversable(SegmentConditionState condition)
        {
            return condition.PassabilityIndex >= MinimumTraversablePassability
                && condition.ClosureRiskIndex < MaximumClosureRisk;
        }

        private static CityRouteDto CreateUnreachable(
            Guid cityId,
            string profile,
            CityRoutePointDto from,
            CityRoutePointDto to,
            CityRoadSegmentConditionsSnapshot? segmentConditions,
            string reason)
        {
            return new CityRouteDto(
                CityId: cityId,
                Profile: profile,
                Accessible: false,
                UsedDynamicRoadConditions: segmentConditions is not null,
                EffectiveTickId: segmentConditions?.EffectiveTickId,
                ConditionsLastEvaluatedAtUtc: segmentConditions?.LastEvaluatedAtUtc,
                From: from,
                To: to,
                TotalDistanceMeters: 0m,
                EstimatedTravelTimeMinutes: 0m,
                OverallPassabilityIndex: 0m,
                UnreachableReason: reason,
                Segments: Array.Empty<CityRouteSegmentDto>());
        }

        private sealed record TraversalEdge(
            RoadSegment RoadSegment,
            Guid FromRoadNodeId,
            Guid ToRoadNodeId,
            decimal TraversalMinutes,
            SegmentConditionState Condition);

        private sealed record PreviousStep(
            Guid PreviousRoadNodeId,
            TraversalEdge Edge);

        private sealed record SegmentConditionState(
            decimal PassabilityIndex,
            decimal SpeedMultiplierIndex,
            decimal SlipRiskIndex,
            decimal ClosureRiskIndex)
        {
            public static SegmentConditionState Neutral => new(
                PassabilityIndex: 1m,
                SpeedMultiplierIndex: 1m,
                SlipRiskIndex: 0m,
                ClosureRiskIndex: 0m);
        }
    }
}
