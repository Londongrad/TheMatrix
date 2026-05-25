using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    GetCityDistrictUtilityIncidentConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.UtilityIncidents.
    GetCityDistrictUtilityIncidentConditions
{
    public sealed class GetCityDistrictUtilityIncidentConditionsQueryHandlerTests
    {
        private static readonly Guid FirstDistrictId = Guid.Parse("75000000-0000-0000-0000-000000000001");
        private static readonly Guid SecondDistrictId = Guid.Parse("75000000-0000-0000-0000-000000000002");

        [Fact]
        public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
        {
            var repository = new FakeCityEnvironmentalConditionRepository();
            var topologyClient = new FakeCityMapTopologyClient();
            GetCityDistrictUtilityIncidentConditionsQueryHandler handler = CreateHandler(
                repository: repository,
                topologyClient: topologyClient);

            CityDistrictUtilityIncidentConditionsDto? result = await handler.Handle(
                request: new GetCityDistrictUtilityIncidentConditionsQuery(
                    SimulationSystemsApplicationTestSupport.CityId),
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
            GetCityDistrictUtilityIncidentConditionsQueryHandler handler = CreateHandler(
                repository: repository,
                topologyClient: topologyClient);

            CityDistrictUtilityIncidentConditionsDto? result = await handler.Handle(
                request: new GetCityDistrictUtilityIncidentConditionsQuery(
                    SimulationSystemsApplicationTestSupport.CityId),
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
                        AnchorX: 24m,
                        AnchorY: 12m))
            };
            var profileFactory = new ClassicCityWeatherPressureProfileFactory();
            var handler = new GetCityDistrictUtilityIncidentConditionsQueryHandler(
                repository: repository,
                cityMapTopologyClient: topologyClient,
                pressureProfileFactory: profileFactory,
                heatingProjectionPolicy: new ClassicCityDistrictHeatingProjectionPolicy(),
                waterProjectionPolicy: new ClassicCityDistrictWaterDistributionProjectionPolicy(),
                powerProjectionPolicy: new ClassicCityDistrictPowerDistributionProjectionPolicy(),
                sanitationProjectionPolicy: new ClassicCityDistrictSanitationProjectionPolicy(),
                projectionPolicy: new ClassicCityDistrictUtilityIncidentProjectionPolicy());

            CityDistrictUtilityIncidentConditionsDto? result = await handler.Handle(
                request: new GetCityDistrictUtilityIncidentConditionsQuery(
                    SimulationSystemsApplicationTestSupport.CityId),
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
                   .UtilityIncidentSupport,
                actual: result.UtilityIncidentSupportIndex);
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
            Assert.True(result.Districts[0].RestorationPriorityIndex >= result.Districts[1].RestorationPriorityIndex);
            Assert.All(
                collection: result.Districts,
                action: district =>
                {
                    Assert.InRange(
                        actual: district.UtilityContinuityIndex,
                        low: 0m,
                        high: 1m);
                    Assert.InRange(
                        actual: district.DispatchReadinessIndex,
                        low: 0m,
                        high: 1m);
                    Assert.InRange(
                        actual: district.IncidentPressureIndex,
                        low: 0m,
                        high: 1m);
                    Assert.InRange(
                        actual: district.CoordinationDifficultyIndex,
                        low: 0m,
                        high: 1m);
                    Assert.InRange(
                        actual: district.RestorationPriorityIndex,
                        low: 0m,
                        high: 1m);
                });
        }

        private static GetCityDistrictUtilityIncidentConditionsQueryHandler CreateHandler(
            FakeCityEnvironmentalConditionRepository repository,
            FakeCityMapTopologyClient topologyClient)
        {
            return new GetCityDistrictUtilityIncidentConditionsQueryHandler(
                repository: repository,
                cityMapTopologyClient: topologyClient,
                pressureProfileFactory: new ClassicCityWeatherPressureProfileFactory(),
                heatingProjectionPolicy: new ClassicCityDistrictHeatingProjectionPolicy(),
                waterProjectionPolicy: new ClassicCityDistrictWaterDistributionProjectionPolicy(),
                powerProjectionPolicy: new ClassicCityDistrictPowerDistributionProjectionPolicy(),
                sanitationProjectionPolicy: new ClassicCityDistrictSanitationProjectionPolicy(),
                projectionPolicy: new ClassicCityDistrictUtilityIncidentProjectionPolicy());
        }
    }
}
