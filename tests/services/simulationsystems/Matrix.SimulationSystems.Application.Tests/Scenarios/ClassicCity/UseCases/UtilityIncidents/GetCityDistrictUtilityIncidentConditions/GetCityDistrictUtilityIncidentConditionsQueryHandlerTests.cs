using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityDistrictUtilityIncidentConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityDistrictUtilityIncidentConditions;

public sealed class GetCityDistrictUtilityIncidentConditionsQueryHandlerTests
{
    private static readonly Guid FirstDistrictId = Guid.Parse("75000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondDistrictId = Guid.Parse("75000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var topologyClient = new FakeCityMapTopologyClient();
        var handler = CreateHandler(repository, topologyClient);

        var result = await handler.Handle(
            new GetCityDistrictUtilityIncidentConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CreateHostId(), repository.RequestedSimulationHostId);
        Assert.Equal(0, topologyClient.GetRoadGraphCallCount);
    }

    [Fact]
    public async Task Handle_WhenTopologyDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository
        {
            State = SimulationSystemsApplicationTestSupport.CreateState()
        };
        var topologyClient = new FakeCityMapTopologyClient();
        var handler = CreateHandler(repository, topologyClient);

        var result = await handler.Handle(
            new GetCityDistrictUtilityIncidentConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, topologyClient.RequestedCityId);
        Assert.Equal(1, topologyClient.GetRoadGraphCallCount);
    }

    [Fact]
    public async Task Handle_WhenStateAndTopologyExist_ReturnsProjectedDistrictConditions()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var topologyClient = new FakeCityMapTopologyClient
        {
            Topology = SimulationSystemsApplicationTestSupport.CreateTopology(
                new CityDistrictTopologyDto(FirstDistrictId, 0m, 0m),
                new CityDistrictTopologyDto(SecondDistrictId, 24m, 12m))
        };
        var profileFactory = new ClassicCityWeatherPressureProfileFactory();
        var handler = new GetCityDistrictUtilityIncidentConditionsQueryHandler(
            repository,
            topologyClient,
            profileFactory,
            new ClassicCityDistrictHeatingProjectionPolicy(),
            new ClassicCityDistrictWaterDistributionProjectionPolicy(),
            new ClassicCityDistrictPowerDistributionProjectionPolicy(),
            new ClassicCityDistrictSanitationProjectionPolicy(),
            new ClassicCityDistrictUtilityIncidentProjectionPolicy());

        var result = await handler.Handle(
            new GetCityDistrictUtilityIncidentConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastAppliedTickId, result.EffectiveTickId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(profileFactory.Create(state).UtilityIncidentSupport, result.UtilityIncidentSupportIndex);
        Assert.Equal(2, result.Districts.Count);
        Assert.Equal(
            [FirstDistrictId, SecondDistrictId],
            result.Districts.Select(x => x.DistrictId).OrderBy(x => x).ToArray());
        Assert.True(result.Districts[0].RestorationPriorityIndex >= result.Districts[1].RestorationPriorityIndex);
        Assert.All(result.Districts, district =>
        {
            Assert.InRange(district.UtilityContinuityIndex, 0m, 1m);
            Assert.InRange(district.DispatchReadinessIndex, 0m, 1m);
            Assert.InRange(district.IncidentPressureIndex, 0m, 1m);
            Assert.InRange(district.CoordinationDifficultyIndex, 0m, 1m);
            Assert.InRange(district.RestorationPriorityIndex, 0m, 1m);
        });
    }

    private static GetCityDistrictUtilityIncidentConditionsQueryHandler CreateHandler(
        FakeCityEnvironmentalConditionRepository repository,
        FakeCityMapTopologyClient topologyClient)
    {
        return new GetCityDistrictUtilityIncidentConditionsQueryHandler(
            repository,
            topologyClient,
            new ClassicCityWeatherPressureProfileFactory(),
            new ClassicCityDistrictHeatingProjectionPolicy(),
            new ClassicCityDistrictWaterDistributionProjectionPolicy(),
            new ClassicCityDistrictPowerDistributionProjectionPolicy(),
            new ClassicCityDistrictSanitationProjectionPolicy(),
            new ClassicCityDistrictUtilityIncidentProjectionPolicy());
    }
}
