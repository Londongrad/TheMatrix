using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.Common;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Simulation;
using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip
{
    public sealed class DispatchCityTripCommandHandler(
        ICityRepository cityRepository,
        ISimulationClockRepository clockRepository,
        IRoadNodeRepository roadNodeRepository,
        ICityActiveTripRepository tripRepository,
        IMediator mediator,
        IUnitOfWork unitOfWork) : IRequestHandler<DispatchCityTripCommand, DispatchCityTripResult>
    {
        public async Task<DispatchCityTripResult> Handle(
            DispatchCityTripCommand request,
            CancellationToken cancellationToken)
        {
            var cityId = new CityId(request.CityId);

            City? city = await cityRepository.GetByIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (city is null)
            {
                return new DispatchCityTripResult(
                    Status: DispatchCityTripStatus.CityNotFound,
                    Trip: null,
                    FailureReason: "City was not found.");
            }

            if (city.IsArchived || city.IsProvisioning)
            {
                return new DispatchCityTripResult(
                    Status: DispatchCityTripStatus.CityNotReady,
                    Trip: null,
                    FailureReason: "Trips can be dispatched only for active cities.");
            }

            SimulationClock? clock = await clockRepository.GetBySimulationIdAsync(
                simulationId: new SimulationId(request.CityId),
                cancellationToken: cancellationToken);

            if (clock is null)
            {
                return new DispatchCityTripResult(
                    Status: DispatchCityTripStatus.CityNotReady,
                    Trip: null,
                    FailureReason: "Simulation clock is not available for this city.");
            }

            CityRouteDto? route = await mediator.Send(
                request: new ResolveCityRouteQuery(
                    CityId: request.CityId,
                    FromKind: request.FromKind,
                    FromId: request.FromId,
                    ToKind: request.ToKind,
                    ToId: request.ToId,
                    Profile: request.Profile),
                cancellationToken: cancellationToken);

            if (route is null)
            {
                return new DispatchCityTripResult(
                    Status: DispatchCityTripStatus.RouteUnavailable,
                    Trip: null,
                    FailureReason: "Trip route could not be resolved for the selected points.");
            }

            if (!route.Accessible)
            {
                return new DispatchCityTripResult(
                    Status: DispatchCityTripStatus.RouteUnavailable,
                    Trip: null,
                    FailureReason: route.UnreachableReason ?? "No accessible route is currently available.");
            }

            IReadOnlyList<RoadNode> roadNodes = await roadNodeRepository.ListByCityIdAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);
            Dictionary<Guid, RoadNode> roadNodeById = roadNodes.ToDictionary(
                keySelector: x => x.Id.Value,
                elementSelector: x => x);

            IReadOnlyList<CityActiveTripSegment> segments = route.Segments
               .Select((segment, index) => CreateSegmentSnapshot(
                    segment: segment,
                    sequence: index,
                    roadNodeById: roadNodeById))
               .ToArray();

            string normalizedPurpose = CityTripPurposeNames.Normalize(request.Purpose);
            string normalizedProfile = CityRouteProfiles.Normalize(request.Profile);
            string subject = string.IsNullOrWhiteSpace(request.Subject)
                ? CityTripPurposeNames.ResolveDefaultSubject(normalizedPurpose)
                : request.Subject.Trim();

            CityActiveTrip trip = CityActiveTrip.Create(
                cityId: cityId,
                travellerEntityId: request.TravellerEntityId,
                subject: subject,
                purpose: CityTripPurposeNames.ToDomain(normalizedPurpose),
                profile: normalizedProfile,
                movementCapabilityIndex: request.MovementCapabilityIndex,
                usedDynamicRoadConditions: route.UsedDynamicRoadConditions,
                plannedAtTickId: clock.TickId.Value,
                conditionsEffectiveTickId: route.EffectiveTickId,
                startedAtSimTimeUtc: clock.CurrentTime.ValueUtc,
                fromKind: route.From.Kind,
                fromEntityId: route.From.EntityId,
                fromDistrictId: new DistrictId(route.From.DistrictId),
                fromRoadNodeId: new RoadNodeId(route.From.RoadNodeId),
                fromName: route.From.Name,
                fromPositionX: route.From.PositionX,
                fromPositionY: route.From.PositionY,
                toKind: route.To.Kind,
                toEntityId: route.To.EntityId,
                toDistrictId: new DistrictId(route.To.DistrictId),
                toRoadNodeId: new RoadNodeId(route.To.RoadNodeId),
                toName: route.To.Name,
                toPositionX: route.To.PositionX,
                toPositionY: route.To.PositionY,
                totalDistanceMeters: route.TotalDistanceMeters,
                plannedTravelTimeMinutes: route.EstimatedTravelTimeMinutes,
                segments: segments);

            await tripRepository.AddAsync(
                trip: trip,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new DispatchCityTripResult(
                Status: DispatchCityTripStatus.Created,
                Trip: CityActiveTripMappings.ToDto(trip),
                FailureReason: null);
        }

        private static CityActiveTripSegment CreateSegmentSnapshot(
            CityRouteSegmentDto segment,
            int sequence,
            IReadOnlyDictionary<Guid, RoadNode> roadNodeById)
        {
            if (!roadNodeById.TryGetValue(segment.FromRoadNodeId, out RoadNode? fromRoadNode)
             || !roadNodeById.TryGetValue(segment.ToRoadNodeId, out RoadNode? toRoadNode))
            {
                throw new InvalidOperationException(
                    $"Road-node coordinates are missing for route segment '{segment.RoadSegmentId}'.");
            }

            return CityActiveTripSegment.Create(
                sequence: sequence,
                roadSegmentId: new RoadSegmentId(segment.RoadSegmentId),
                districtId: new DistrictId(segment.DistrictId),
                fromRoadNodeId: new RoadNodeId(segment.FromRoadNodeId),
                toRoadNodeId: new RoadNodeId(segment.ToRoadNodeId),
                name: segment.Name,
                type: segment.Type,
                lengthMeters: segment.LengthMeters,
                estimatedTraversalMinutes: segment.EstimatedTraversalMinutes,
                fromPositionX: fromRoadNode.PositionX,
                fromPositionY: fromRoadNode.PositionY,
                toPositionX: toRoadNode.PositionX,
                toPositionY: toRoadNode.PositionY);
        }
    }
}
