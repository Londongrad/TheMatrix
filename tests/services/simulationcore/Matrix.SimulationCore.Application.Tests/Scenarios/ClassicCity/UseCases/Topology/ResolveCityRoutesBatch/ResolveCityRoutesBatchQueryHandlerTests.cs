using FluentValidation.Results;
using Matrix.SimulationCore.Application.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Abstractions.Persistence;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.Services.Routing.Abstractions;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoute;
using Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoutesBatch;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Topology.Enums;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.Scenarios.ClassicCity.UseCases.Topology.ResolveCityRoutesBatch
{
    public sealed class ResolveCityRoutesBatchQueryHandlerTests
    {
        private static readonly Guid CityGuid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly CityId TestCityId = new(CityGuid);
        private static readonly DistrictId TestDistrictId = new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        private static readonly DateTimeOffset CreatedAtUtc = new(
            year: 2030,
            month: 1,
            day: 1,
            hour: 0,
            minute: 0,
            second: 0,
            offset: TimeSpan.Zero);

        [Fact]
        public void Validate_WhenBatchIsEmpty_ReturnsError()
        {
            var validator = new ResolveCityRoutesBatchQueryValidator();

            ValidationResult? result = validator.Validate(
                new ResolveCityRoutesBatchQuery(
                    CityId: CityGuid,
                    Routes: []));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Routes");
        }

        [Fact]
        public void Validate_WhenBatchIsTooLarge_ReturnsError()
        {
            var validator = new ResolveCityRoutesBatchQueryValidator();
            ResolveCityRoutesBatchQueryItem[] routes = Enumerable.Range(
                    start: 0,
                    count: 513)
               .Select(index => CreateQueryItem(
                    index: index,
                    fromId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    toId: Guid.Parse("22222222-2222-2222-2222-222222222222")))
               .ToArray();

            ValidationResult? result = validator.Validate(
                new ResolveCityRoutesBatchQuery(
                    CityId: CityGuid,
                    Routes: routes));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Routes");
        }

        [Fact]
        public async Task Handle_LoadsTopologyOnceAndReturnsOneResponsePerInputRoute()
        {
            TestTopology topology = CreateTopology(
                buildingCount: 2,
                anchorCount: 2);
            FakeRoadNodeRepository roadNodeRepository = new(topology.RoadNodes);
            FakeRoadSegmentRepository roadSegmentRepository = new([]);
            FakeResidentialBuildingRepository residentialBuildingRepository = new(topology.Buildings);
            FakeCityAnchorRepository cityAnchorRepository = new(topology.Anchors);
            FakeRoadSegmentConditionsClient conditionsClient = new();
            FakeRoutePlanner routePlanner = new();
            ResolveCityRoutesBatchQueryHandler handler = CreateHandler(
                roadNodeRepository: roadNodeRepository,
                roadSegmentRepository: roadSegmentRepository,
                residentialBuildingRepository: residentialBuildingRepository,
                cityAnchorRepository: cityAnchorRepository,
                conditionsClient: conditionsClient,
                routePlanner: routePlanner);

            ResolveCityRoutesBatchResult result = await handler.Handle(
                request: new ResolveCityRoutesBatchQuery(
                    CityId: CityGuid,
                    Routes:
                    [
                        CreateQueryItem(
                            index: 0,
                            fromId: topology.Buildings[0].Id.Value,
                            toId: topology.Anchors[0].Id.Value),
                        CreateQueryItem(
                            index: 1,
                            fromId: topology.Buildings[1].Id.Value,
                            toId: topology.Anchors[1].Id.Value)
                    ]),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 2,
                actual: result.Routes.Count);
            Assert.Equal(
                expected: 0,
                actual: result.Routes[0].Index);
            Assert.Equal(
                expected: 1,
                actual: result.Routes[1].Index);
            Assert.Equal(
                expected: 1,
                actual: roadNodeRepository.ListCalls);
            Assert.Equal(
                expected: 1,
                actual: roadSegmentRepository.ListCalls);
            Assert.Equal(
                expected: 1,
                actual: residentialBuildingRepository.ListCalls);
            Assert.Equal(
                expected: 0,
                actual: residentialBuildingRepository.GetByIdCalls);
            Assert.Equal(
                expected: 1,
                actual: cityAnchorRepository.ListCalls);
            Assert.Equal(
                expected: 0,
                actual: cityAnchorRepository.GetByIdCalls);
            Assert.Equal(
                expected: 1,
                actual: conditionsClient.GetByCityIdCalls);
        }

        [Fact]
        public async Task Handle_PreservesInputOrder()
        {
            TestTopology topology = CreateTopology(
                buildingCount: 3,
                anchorCount: 3);
            ResolveCityRoutesBatchQueryHandler handler = CreateHandler(topology);

            ResolveCityRoutesBatchResult result = await handler.Handle(
                request: new ResolveCityRoutesBatchQuery(
                    CityId: CityGuid,
                    Routes:
                    [
                        CreateQueryItem(
                            index: 0,
                            fromId: topology.Buildings[2].Id.Value,
                            toId: topology.Anchors[2].Id.Value),
                        CreateQueryItem(
                            index: 1,
                            fromId: topology.Buildings[0].Id.Value,
                            toId: topology.Anchors[0].Id.Value),
                        CreateQueryItem(
                            index: 2,
                            fromId: topology.Buildings[1].Id.Value,
                            toId: topology.Anchors[1].Id.Value)
                    ]),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 3,
                actual: result.Routes.Count);
            Assert.Equal(
                expected: 0,
                actual: result.Routes[0].Index);
            Assert.Equal(
                expected: topology.Buildings[2].Id.Value,
                actual: result.Routes[0].Route!.From.EntityId);
            Assert.Equal(
                expected: 1,
                actual: result.Routes[1].Index);
            Assert.Equal(
                expected: topology.Buildings[0].Id.Value,
                actual: result.Routes[1].Route!.From.EntityId);
            Assert.Equal(
                expected: 2,
                actual: result.Routes[2].Index);
            Assert.Equal(
                expected: topology.Buildings[1].Id.Value,
                actual: result.Routes[2].Route!.From.EntityId);
        }

        [Fact]
        public async Task Handle_WhenPointCannotBeResolved_ReturnsNullRouteForThatItem()
        {
            TestTopology topology = CreateTopology(
                buildingCount: 1,
                anchorCount: 1);
            ResolveCityRoutesBatchQueryHandler handler = CreateHandler(topology);

            ResolveCityRoutesBatchResult result = await handler.Handle(
                request: new ResolveCityRoutesBatchQuery(
                    CityId: CityGuid,
                    Routes:
                    [
                        CreateQueryItem(
                            index: 0,
                            fromId: topology.Buildings[0].Id.Value,
                            toId: topology.Anchors[0].Id.Value),
                        CreateQueryItem(
                            index: 1,
                            fromId: Guid.Parse("99999999-9999-9999-9999-999999999999"),
                            toId: topology.Anchors[0].Id.Value)
                    ]),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result.Routes[0].Route);
            Assert.Null(result.Routes[1].Route);
        }

        [Fact]
        public async Task Handle_DeduplicatesIdenticalRoutePlanning()
        {
            TestTopology topology = CreateTopology(
                buildingCount: 1,
                anchorCount: 1);
            FakeRoutePlanner routePlanner = new();
            ResolveCityRoutesBatchQueryHandler handler = CreateHandler(
                topology: topology,
                routePlanner: routePlanner);
            ResolveCityRoutesBatchQueryItem item = CreateQueryItem(
                index: 0,
                fromId: topology.Buildings[0].Id.Value,
                toId: topology.Anchors[0].Id.Value);

            ResolveCityRoutesBatchResult result = await handler.Handle(
                request: new ResolveCityRoutesBatchQuery(
                    CityId: CityGuid,
                    Routes:
                    [
                        item,
                        item with
                        {
                            Index = 1
                        }
                    ]),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: 2,
                actual: result.Routes.Count);
            Assert.NotNull(result.Routes[0].Route);
            Assert.NotNull(result.Routes[1].Route);
            Assert.Equal(
                expected: 1,
                actual: routePlanner.PlanCalls);
        }

        [Fact]
        public async Task Handle_PreservesInaccessibleRoute()
        {
            TestTopology topology = CreateTopology(
                buildingCount: 1,
                anchorCount: 1);
            FakeRoutePlanner routePlanner = new()
            {
                Accessible = false
            };
            ResolveCityRoutesBatchQueryHandler handler = CreateHandler(
                topology: topology,
                routePlanner: routePlanner);

            ResolveCityRoutesBatchResult result = await handler.Handle(
                request: new ResolveCityRoutesBatchQuery(
                    CityId: CityGuid,
                    Routes:
                    [
                        CreateQueryItem(
                            index: 0,
                            fromId: topology.Buildings[0].Id.Value,
                            toId: topology.Anchors[0].Id.Value)
                    ]),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result.Routes[0].Route);
            Assert.False(result.Routes[0].Route!.Accessible);
        }

        private static ResolveCityRoutesBatchQueryHandler CreateHandler(TestTopology topology)
        {
            return CreateHandler(
                topology: topology,
                routePlanner: new FakeRoutePlanner());
        }

        private static ResolveCityRoutesBatchQueryHandler CreateHandler(
            TestTopology topology,
            FakeRoutePlanner routePlanner)
        {
            return CreateHandler(
                roadNodeRepository: new FakeRoadNodeRepository(topology.RoadNodes),
                roadSegmentRepository: new FakeRoadSegmentRepository([]),
                residentialBuildingRepository: new FakeResidentialBuildingRepository(topology.Buildings),
                cityAnchorRepository: new FakeCityAnchorRepository(topology.Anchors),
                conditionsClient: new FakeRoadSegmentConditionsClient(),
                routePlanner: routePlanner);
        }

        private static ResolveCityRoutesBatchQueryHandler CreateHandler(
            FakeRoadNodeRepository roadNodeRepository,
            FakeRoadSegmentRepository roadSegmentRepository,
            FakeResidentialBuildingRepository residentialBuildingRepository,
            FakeCityAnchorRepository cityAnchorRepository,
            FakeRoadSegmentConditionsClient conditionsClient,
            FakeRoutePlanner routePlanner)
        {
            return new ResolveCityRoutesBatchQueryHandler(
                roadNodeRepository: roadNodeRepository,
                roadSegmentRepository: roadSegmentRepository,
                residentialBuildingRepository: residentialBuildingRepository,
                cityAnchorRepository: cityAnchorRepository,
                roadSegmentConditionsClient: conditionsClient,
                routePlanner: routePlanner);
        }

        private static ResolveCityRoutesBatchQueryItem CreateQueryItem(
            int index,
            Guid fromId,
            Guid toId)
        {
            return new ResolveCityRoutesBatchQueryItem(
                Index: index,
                FromKind: CityRouteMapPointKinds.ResidentialBuilding,
                FromId: fromId,
                ToKind: CityRouteMapPointKinds.CityAnchor,
                ToId: toId,
                Profile: CityRouteProfiles.Pedestrian);
        }

        private static TestTopology CreateTopology(
            int buildingCount,
            int anchorCount)
        {
            int nodeCount = Math.Max(
                val1: buildingCount,
                val2: anchorCount);
            List<RoadNode> roadNodes = [];

            for (int i = 0; i < nodeCount; i++)
                roadNodes.Add(CreateRoadNode(index: i + 1));

            List<ResidentialBuilding> buildings = [];
            for (int i = 0; i < buildingCount; i++)
                buildings.Add(
                    CreateResidentialBuilding(
                        index: i + 1,
                        accessRoadNodeId: roadNodes[i].Id));

            List<CityAnchor> anchors = [];
            for (int i = 0; i < anchorCount; i++)
                anchors.Add(
                    CreateCityAnchor(
                        index: i + 1,
                        accessRoadNodeId: roadNodes[i].Id));

            return new TestTopology(
                RoadNodes: roadNodes,
                Buildings: buildings,
                Anchors: anchors);
        }

        private static RoadNode CreateRoadNode(int index)
        {
            return RoadNode.Create(
                cityId: TestCityId,
                districtId: TestDistrictId,
                name: $"Node {index}",
                type: RoadNodeType.Junction,
                positionX: index,
                positionY: index + 1,
                createdAtUtc: CreatedAtUtc);
        }

        private static ResidentialBuilding CreateResidentialBuilding(
            int index,
            RoadNodeId accessRoadNodeId)
        {
            return ResidentialBuilding.Create(
                cityId: TestCityId,
                districtId: TestDistrictId,
                accessRoadNodeId: accessRoadNodeId,
                name: new ResidentialBuildingName($"Home {index}"),
                type: ResidentialBuildingType.ApartmentBlock,
                residentCapacity: ResidentCapacity.From(100),
                positionX: index + 10,
                positionY: index + 11,
                createdAtUtc: CreatedAtUtc);
        }

        private static CityAnchor CreateCityAnchor(
            int index,
            RoadNodeId accessRoadNodeId)
        {
            return CityAnchor.Create(
                cityId: TestCityId,
                districtId: TestDistrictId,
                accessRoadNodeId: accessRoadNodeId,
                name: new CityAnchorName($"Anchor {index}"),
                type: CityAnchorType.Workplace,
                capacity: 100,
                positionX: index + 20,
                positionY: index + 21,
                createdAtUtc: CreatedAtUtc);
        }

        private sealed record TestTopology(
            IReadOnlyList<RoadNode> RoadNodes,
            IReadOnlyList<ResidentialBuilding> Buildings,
            IReadOnlyList<CityAnchor> Anchors);

        private sealed class FakeRoadNodeRepository(IReadOnlyList<RoadNode> roadNodes) : IRoadNodeRepository
        {
            public int ListCalls { get; private set; }

            public Task<IReadOnlyList<RoadNode>> ListByCityIdAsync(
                CityId cityId,
                CancellationToken cancellationToken)
            {
                ListCalls++;
                return Task.FromResult(roadNodes);
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<RoadNode> roadNodes,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class FakeRoadSegmentRepository(IReadOnlyList<RoadSegment> roadSegments) : IRoadSegmentRepository
        {
            public int ListCalls { get; private set; }

            public Task<IReadOnlyList<RoadSegment>> ListByCityIdAsync(
                CityId cityId,
                CancellationToken cancellationToken)
            {
                ListCalls++;
                return Task.FromResult(roadSegments);
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<RoadSegment> roadSegments,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class FakeResidentialBuildingRepository(IReadOnlyList<ResidentialBuilding> buildings)
            : IResidentialBuildingRepository
        {
            public int ListCalls { get; private set; }
            public int GetByIdCalls { get; private set; }

            public Task<ResidentialBuilding?> GetByIdAsync(
                ResidentialBuildingId buildingId,
                CancellationToken cancellationToken)
            {
                GetByIdCalls++;
                return Task.FromResult<ResidentialBuilding?>(null);
            }

            public Task<IReadOnlyList<ResidentialBuilding>> ListByCityIdAsync(
                CityId cityId,
                DistrictId? districtId,
                CancellationToken cancellationToken)
            {
                ListCalls++;
                return Task.FromResult(buildings);
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<ResidentialBuilding> buildings,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class FakeCityAnchorRepository(IReadOnlyList<CityAnchor> anchors) : ICityAnchorRepository
        {
            public int ListCalls { get; private set; }
            public int GetByIdCalls { get; private set; }

            public Task<CityAnchor?> GetByIdAsync(
                CityAnchorId anchorId,
                CancellationToken cancellationToken)
            {
                GetByIdCalls++;
                return Task.FromResult<CityAnchor?>(null);
            }

            public Task<IReadOnlyList<CityAnchor>> ListByCityIdAsync(
                CityId cityId,
                CancellationToken cancellationToken)
            {
                ListCalls++;
                return Task.FromResult(anchors);
            }

            public Task AddRangeAsync(
                IReadOnlyCollection<CityAnchor> anchors,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }
        }

        private sealed class FakeRoadSegmentConditionsClient : ICityRoadSegmentConditionsClient
        {
            public int GetByCityIdCalls { get; private set; }

            public Task<CityRoadSegmentConditionsSnapshot?> GetByCityIdAsync(
                Guid cityId,
                CancellationToken cancellationToken)
            {
                GetByCityIdCalls++;
                return Task.FromResult<CityRoadSegmentConditionsSnapshot?>(null);
            }
        }

        private sealed class FakeRoutePlanner : IClassicCityRoutePlanner
        {
            public int PlanCalls { get; private set; }
            public bool Accessible { get; set; } = true;

            public CityRouteDto Plan(
                Guid cityId,
                string profile,
                CityRoutePointDto from,
                CityRoutePointDto to,
                IReadOnlyList<RoadNode> roadNodes,
                IReadOnlyList<RoadSegment> roadSegments,
                CityRoadSegmentConditionsSnapshot? segmentConditions)
            {
                PlanCalls++;
                return new CityRouteDto(
                    CityId: cityId,
                    Profile: profile,
                    Accessible: Accessible,
                    UsedDynamicRoadConditions: segmentConditions is not null,
                    EffectiveTickId: segmentConditions?.EffectiveTickId,
                    ConditionsLastEvaluatedAtUtc: segmentConditions?.LastEvaluatedAtUtc,
                    From: from,
                    To: to,
                    TotalDistanceMeters: 100m + PlanCalls,
                    EstimatedTravelTimeMinutes: 10m + PlanCalls,
                    OverallPassabilityIndex: Accessible
                        ? 0.9m
                        : 0.1m,
                    UnreachableReason: Accessible
                        ? null
                        : "Blocked",
                    Segments: []);
            }
        }
    }
}
