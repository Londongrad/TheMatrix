using System.Data;
using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.World.DispatchCityTrip;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.World.Enums;
using MediatR;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.World
{
    internal static class WorldTestSupport
    {
        internal static readonly DateTimeOffset StartedAtUtc = new(
            year: 2048,
            month: 5,
            day: 6,
            hour: 7,
            minute: 8,
            second: 9,
            offset: TimeSpan.Zero);

        internal static CityActiveTrip CreateActiveTrip(
            CityId? cityId = null,
            string subject = "Morning commute")
        {
            CityId actualCityId = cityId ?? new CityId(Guid.NewGuid());
            DistrictId districtId = new(Guid.NewGuid());
            var fromRoadNodeId = RoadNodeId.New();
            var toRoadNodeId = RoadNodeId.New();
            var roadSegmentId = RoadSegmentId.New();
            IReadOnlyCollection<CityActiveTripSegment> segments =
            [
                CityActiveTripSegment.Create(
                    sequence: 0,
                    roadSegmentId: roadSegmentId,
                    districtId: districtId,
                    fromRoadNodeId: fromRoadNodeId,
                    toRoadNodeId: toRoadNodeId,
                    name: "Downtown Connector",
                    type: "Collector",
                    lengthMeters: 320m,
                    estimatedTraversalMinutes: 8m,
                    fromPositionX: 10m,
                    fromPositionY: 20m,
                    toPositionX: 40m,
                    toPositionY: 60m)
            ];

            return CityActiveTrip.Create(
                cityId: actualCityId,
                travellerEntityId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                subject: subject,
                purpose: CityTripPurpose.WorkCommute,
                profile: "Pedestrian",
                movementCapabilityIndex: 1m,
                usedDynamicRoadConditions: true,
                plannedAtTickId: 24,
                conditionsEffectiveTickId: 21,
                startedAtSimTimeUtc: StartedAtUtc,
                fromKind: "ResidentialBuilding",
                fromEntityId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                fromDistrictId: districtId,
                fromRoadNodeId: fromRoadNodeId,
                fromName: "River Tower",
                fromPositionX: 10m,
                fromPositionY: 20m,
                toKind: "CityAnchor",
                toEntityId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                toDistrictId: districtId,
                toRoadNodeId: toRoadNodeId,
                toName: "Central Hospital",
                toPositionX: 40m,
                toPositionY: 60m,
                totalDistanceMeters: 320m,
                plannedTravelTimeMinutes: 8m,
                segments: segments);
        }

        internal static DispatchCityTripCommand CreateDispatchCommand(
            Guid cityId,
            Guid fromId,
            Guid toId,
            string purpose = "ServiceResponse",
            string profile = "ServiceVehicle",
            string? subject = null)
        {
            return new DispatchCityTripCommand(
                CityId: cityId,
                FromKind: "ResidentialBuilding",
                FromId: fromId,
                ToKind: "CityAnchor",
                ToId: toId,
                Purpose: purpose,
                Profile: profile,
                MovementCapabilityIndex: 1.15m,
                TravellerEntityId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Subject: subject);
        }

        internal static CityRouteDto CreateRoute(
            Guid cityId,
            Guid fromDistrictId,
            Guid fromRoadNodeId,
            Guid fromEntityId,
            Guid toDistrictId,
            Guid toRoadNodeId,
            Guid toEntityId,
            Guid roadSegmentId,
            bool accessible = true,
            string? unreachableReason = null)
        {
            return new CityRouteDto(
                CityId: cityId,
                Profile: "ServiceVehicle",
                Accessible: accessible,
                UsedDynamicRoadConditions: true,
                EffectiveTickId: 30,
                ConditionsLastEvaluatedAtUtc: StartedAtUtc,
                From: new CityRoutePointDto(
                    Kind: "ResidentialBuilding",
                    EntityId: fromEntityId,
                    DistrictId: fromDistrictId,
                    RoadNodeId: fromRoadNodeId,
                    Name: "River Tower",
                    PositionX: 10m,
                    PositionY: 20m),
                To: new CityRoutePointDto(
                    Kind: "CityAnchor",
                    EntityId: toEntityId,
                    DistrictId: toDistrictId,
                    RoadNodeId: toRoadNodeId,
                    Name: "Central Hospital",
                    PositionX: 40m,
                    PositionY: 60m),
                TotalDistanceMeters: 320m,
                EstimatedTravelTimeMinutes: 8m,
                OverallPassabilityIndex: 0.91m,
                UnreachableReason: unreachableReason,
                Segments:
                [
                    new CityRouteSegmentDto(
                        RoadSegmentId: roadSegmentId,
                        DistrictId: fromDistrictId,
                        FromRoadNodeId: fromRoadNodeId,
                        ToRoadNodeId: toRoadNodeId,
                        Name: "Downtown Connector",
                        Type: "Collector",
                        LengthMeters: 320m,
                        EstimatedTraversalMinutes: 8m,
                        PassabilityIndex: 0.91m,
                        SpeedMultiplierIndex: 0.88m,
                        SlipRiskIndex: 0.05m,
                        ClosureRiskIndex: 0.03m)
                ]);
        }

        internal sealed class FakeCityActiveTripRepository : ICityActiveTripRepository
        {
            public IReadOnlyList<CityActiveTrip> Trips { get; set; } = Array.Empty<CityActiveTrip>();
            public CityId? RequestedCityId { get; private set; }
            public CityId? RequestedUpdateCityId { get; private set; }
            public CityActiveTrip? AddedTrip { get; private set; }

            public Task AddAsync(
                CityActiveTrip trip,
                CancellationToken cancellationToken)
            {
                AddedTrip = trip;
                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<CityActiveTrip>> ListActiveForUpdateByCityIdAsync(
                CityId cityId,
                CancellationToken cancellationToken)
            {
                RequestedUpdateCityId = cityId;
                return Task.FromResult(Trips);
            }

            public Task<IReadOnlyList<CityActiveTrip>> ListActiveByCityIdAsync(
                CityId cityId,
                CancellationToken cancellationToken)
            {
                RequestedCityId = cityId;
                return Task.FromResult(Trips);
            }
        }

        internal sealed class FakeMediator : IMediator
        {
            public object? Requested { get; private set; }
            public object? Response { get; set; }

            public Task Publish(
                object notification,
                CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task Publish<TNotification>(
                TNotification notification,
                CancellationToken cancellationToken = default)
                where TNotification : INotification
            {
                return Task.CompletedTask;
            }

            public Task<TResponse> Send<TResponse>(
                IRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                Requested = request;
                return Task.FromResult((TResponse)Response!);
            }

            public Task Send<TRequest>(
                TRequest request,
                CancellationToken cancellationToken = default)
                where TRequest : IRequest
            {
                Requested = request;
                return Task.CompletedTask;
            }

            public Task<object?> Send(
                object request,
                CancellationToken cancellationToken = default)
            {
                Requested = request;
                return Task.FromResult(Response);
            }

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
                IStreamRequest<TResponse> request,
                CancellationToken cancellationToken = default)
            {
                Requested = request;
                return EmptyAsync<TResponse>();
            }

            public IAsyncEnumerable<object?> CreateStream(
                object request,
                CancellationToken cancellationToken = default)
            {
                Requested = request;
                return EmptyAsync<object?>();
            }

            private static async IAsyncEnumerable<T> EmptyAsync<T>()
            {
                await Task.CompletedTask;
                yield break;
            }
        }

        internal sealed class FakeUnitOfWork : IUnitOfWork
        {
            public int SaveChangesCallCount { get; private set; }

            public Task SaveChangesAsync(CancellationToken cancellationToken)
            {
                SaveChangesCallCount++;
                return Task.CompletedTask;
            }

            public Task ExecuteInTransactionAsync(
                Func<CancellationToken, Task> action,
                CancellationToken cancellationToken,
                IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                throw new NotSupportedException();
            }

            public Task<T> ExecuteInTransactionAsync<T>(
                Func<CancellationToken, Task<T>> action,
                CancellationToken cancellationToken,
                IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
            {
                throw new NotSupportedException();
            }
        }
    }
}
