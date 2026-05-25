using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Heating.
    GetCityDistrictHeatingConditions
{
    public sealed class GetCityDistrictHeatingConditionsQueryHandlerTests
    {
        private static readonly Guid FirstDistrictId = Guid.Parse("77000000-0000-0000-0000-000000000001");
        private static readonly Guid SecondDistrictId = Guid.Parse("77000000-0000-0000-0000-000000000002");

        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var topologyClient = new FakeCityMapTopologyClient();
            GetCityDistrictHeatingConditionsQueryHandler handler = CreateHandler(
                repository: repository,
                topologyClient: topologyClient);

            CityDistrictHeatingConditionsDto? result = await handler.Handle(
                request: new GetCityDistrictHeatingConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
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
            GetCityDistrictHeatingConditionsQueryHandler handler = CreateHandler(
                repository: repository,
                topologyClient: topologyClient);

            CityDistrictHeatingConditionsDto? result = await handler.Handle(
                request: new GetCityDistrictHeatingConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
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
        public async Task Handle_WhenStateAndTopologyExist_ReturnsProjectedDistrictConditions()
        {
            CityEnvironmentalConditionState state = SimulationSystemsApplicationTestSupport.CreateState();
            var repository = new FakeCityEnvironmentalConditionRepository
            {
                State = state
            };
            var topologyClient = new FakeCityMapTopologyClient
            {
                Topology = SimulationSystemsApplicationTestSupport.CreateTopology(
                    new CityDistrictTopologyDto(
                        DistrictId: FirstDistrictId,
                        AnchorX: 0m,
                        AnchorY: 0m),
                    new CityDistrictTopologyDto(
                        DistrictId: SecondDistrictId,
                        AnchorX: 20m,
                        AnchorY: 10m))
            };
            var profileFactory = new ClassicCityWeatherPressureProfileFactory();
            var handler = new GetCityDistrictHeatingConditionsQueryHandler(
                repository: repository,
                cityMapTopologyClient: topologyClient,
                pressureProfileFactory: profileFactory,
                projectionPolicy: new ClassicCityDistrictHeatingProjectionPolicy());

            CityDistrictHeatingConditionsDto? result = await handler.Handle(
                request: new GetCityDistrictHeatingConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
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
                   .HeatingSupport,
                actual: result.HeatingSupportIndex);
            Assert.Equal(
                expected: 2,
                actual: result.Districts.Count);
            Assert.Equal(
                expectedSpan:
                [
                    FirstDistrictId,
                    SecondDistrictId
                ],
                actualArray: result.Districts.Select(x => x.DistrictId)
                   .OrderBy(x => x)
                   .ToArray());
            Assert.True(result.Districts[0].MaintenancePriorityIndex >= result.Districts[1].MaintenancePriorityIndex);
            Assert.All(
                collection: result.Districts,
                action: district =>
                {
                    Assert.InRange(
                        actual: district.HeatingCoverageIndex,
                        low: 0m,
                        high: 1m);
                    Assert.InRange(
                        actual: district.HeatingSupportIndex,
                        low: 0m,
                        high: 1m);
                    Assert.InRange(
                        actual: district.OutageRiskIndex,
                        low: 0m,
                        high: 1m);
                    Assert.InRange(
                        actual: district.ComfortStressIndex,
                        low: 0m,
                        high: 1m);
                    Assert.InRange(
                        actual: district.MaintenancePriorityIndex,
                        low: 0m,
                        high: 1m);
                });
        }

        private static GetCityDistrictHeatingConditionsQueryHandler CreateHandler(
            FakeCityEnvironmentalConditionRepository repository,
            FakeCityMapTopologyClient topologyClient)
        {
            return new GetCityDistrictHeatingConditionsQueryHandler(
                repository: repository,
                cityMapTopologyClient: topologyClient,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory(),
                projectionPolicy: new ClassicCityDistrictHeatingProjectionPolicy());
        }
    }
}
