using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.RoadAccess.
    GetCityRoadSegmentConditions
{
    public sealed class GetCityRoadSegmentConditionsQueryHandlerTests
    {
        private static readonly Guid FirstDistrictId = Guid.Parse("76000000-0000-0000-0000-000000000001");
        private static readonly Guid SecondDistrictId = Guid.Parse("76000000-0000-0000-0000-000000000002");
        private static readonly Guid FirstSegmentId = Guid.Parse("76100000-0000-0000-0000-000000000001");
        private static readonly Guid SecondSegmentId = Guid.Parse("76100000-0000-0000-0000-000000000002");

        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var topologyClient = new FakeCityMapTopologyClient();
            GetCityRoadSegmentConditionsQueryHandler handler = CreateHandler(
                repository: repository,
                topologyClient: topologyClient);

            CityRoadSegmentConditionsDto? result = await handler.Handle(
                request: new GetCityRoadSegmentConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CreateHostId(),
                actual: repository.RequestedSimulationHostId);
            Assert.Equal(
                expected: 0,
                actual: topologyClient.GetRoadGraphCallCount);
        }

        [Fact]
        public async Task Handle_WhenTopologyDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = SimulationSystemsApplicationTestSupport.CreateState()
            };
            var topologyClient = new FakeCityMapTopologyClient();
            GetCityRoadSegmentConditionsQueryHandler handler = CreateHandler(
                repository: repository,
                topologyClient: topologyClient);

            CityRoadSegmentConditionsDto? result = await handler.Handle(
                request: new GetCityRoadSegmentConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
                cancellationToken: CancellationToken.None);

            Assert.Null(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: topologyClient.RequestedCityId);
            Assert.Equal(
                expected: 1,
                actual: topologyClient.GetRoadGraphCallCount);
        }

        [Fact]
        public async Task Handle_WhenStateAndTopologyExist_ReturnsProjectedSegmentConditions()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var topologyClient = new FakeCityMapTopologyClient
            {
                Topology = new CityRoadGraphTopologyDto(
                    CityId: SimulationSystemsApplicationTestSupport.CityId,
                    Districts:
                    [
                        new CityDistrictTopologyDto(
                            DistrictId: FirstDistrictId,
                            AnchorX: 0m,
                            AnchorY: 0m),
                        new CityDistrictTopologyDto(
                            DistrictId: SecondDistrictId,
                            AnchorX: 18m,
                            AnchorY: 8m)
                    ],
                    RoadSegments:
                    [
                        new CityRoadSegmentTopologyDto(
                            RoadSegmentId: FirstSegmentId,
                            DistrictId: FirstDistrictId,
                            FromRoadNodeId: Guid.Parse("76200000-0000-0000-0000-000000000001"),
                            ToRoadNodeId: Guid.Parse("76200000-0000-0000-0000-000000000002"),
                            Name: "Aurora Avenue",
                            Type: "Arterial",
                            LengthMeters: 420m),
                        new CityRoadSegmentTopologyDto(
                            RoadSegmentId: SecondSegmentId,
                            DistrictId: SecondDistrictId,
                            FromRoadNodeId: Guid.Parse("76200000-0000-0000-0000-000000000003"),
                            ToRoadNodeId: Guid.Parse("76200000-0000-0000-0000-000000000004"),
                            Name: "Birch Street",
                            Type: "LocalAccess",
                            LengthMeters: 180m)
                    ])
            };
            var profileFactory = new ClassicCityWeatherPressureProfileFactory();
            var handler = new GetCityRoadSegmentConditionsQueryHandler(
                repository: repository,
                cityMapTopologyClient: topologyClient,
                pressureProfileFactory: profileFactory,
                projectionPolicy: new ClassicCityRoadSegmentConditionProjectionPolicy());

            CityRoadSegmentConditionsDto? result = await handler.Handle(
                request: new GetCityRoadSegmentConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
                cancellationToken: CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(
                expected: SimulationSystemsApplicationTestSupport.CityId,
                actual: result!.CityId);
            Assert.Equal(
                expected: state.LastAppliedTickId,
                actual: result.EffectiveTickId);
            Assert.Equal(
                expected: state.LastEvaluatedAtUtc,
                actual: result.LastEvaluatedAtUtc);
            Assert.Equal(
                expected: profileFactory.Create(state)
                   .RoadSupport,
                actual: result.RoadSupportIndex);
            Assert.Equal(
                expected: 2,
                actual: result.Segments.Count);
            Assert.Equal(
                expectedSpan:
                [
                    FirstSegmentId,
                    SecondSegmentId
                ],
                actualArray: result.Segments.Select(x => x.RoadSegmentId)
                   .OrderBy(x => x)
                   .ToArray());
            Assert.True(result.Segments[0].MaintenancePriorityIndex >= result.Segments[1].MaintenancePriorityIndex);
            Assert.All(
                collection: result.Segments,
                action: segment =>
                {
                    Assert.InRange(
                        actual: segment.PassabilityIndex,
                        low: 0m,
                        high: 1m);
                    Assert.InRange(
                        actual: segment.SpeedMultiplierIndex,
                        low: 0m,
                        high: 1.08m);
                    Assert.InRange(
                        actual: segment.SlipRiskIndex,
                        low: 0m,
                        high: 1m);
                    Assert.InRange(
                        actual: segment.ClosureRiskIndex,
                        low: 0m,
                        high: 1m);
                    Assert.InRange(
                        actual: segment.MaintenancePriorityIndex,
                        low: 0m,
                        high: 1m);
                });
        }

        private static GetCityRoadSegmentConditionsQueryHandler CreateHandler(
            FakeCityEnvironmentalConditionRepository repository,
            FakeCityMapTopologyClient topologyClient)
        {
            return new GetCityRoadSegmentConditionsQueryHandler(
                repository: repository,
                cityMapTopologyClient: topologyClient,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory(),
                projectionPolicy: new ClassicCityRoadSegmentConditionProjectionPolicy());
        }
    }
}
