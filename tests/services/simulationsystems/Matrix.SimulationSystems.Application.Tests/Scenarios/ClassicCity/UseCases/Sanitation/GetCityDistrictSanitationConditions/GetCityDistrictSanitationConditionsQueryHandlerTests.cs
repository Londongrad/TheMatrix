using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCityDistrictSanitationConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Sanitation.GetCityDistrictSanitationConditions;

public sealed class GetCityDistrictSanitationConditionsQueryHandlerTests
{
    private static readonly Guid FirstDistrictId = Guid.Parse("79000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondDistrictId = Guid.Parse("79000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var topologyClient = new FakeCityMapTopologyClient();
        var handler = CreateHandler(repository, topologyClient);

        var result = await handler.Handle(
            new GetCityDistrictSanitationConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
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
            new GetCityDistrictSanitationConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
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
                new CityDistrictTopologyDto(SecondDistrictId, 15m, 12m))
        };
        var profileFactory = new ClassicCityWeatherPressureProfileFactory();
        var handler = new GetCityDistrictSanitationConditionsQueryHandler(
            repository,
            topologyClient,
            profileFactory,
            new ClassicCityDistrictSanitationProjectionPolicy());

        var result = await handler.Handle(
            new GetCityDistrictSanitationConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastAppliedTickId, result.EffectiveTickId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(profileFactory.Create(state).SanitationSupport, result.SanitationSupportIndex);
        Assert.Equal(2, result.Districts.Count);
        Assert.Equal(
            [FirstDistrictId, SecondDistrictId],
            result.Districts.Select(x => x.DistrictId).OrderBy(x => x).ToArray());
        Assert.True(result.Districts[0].MaintenancePriorityIndex >= result.Districts[1].MaintenancePriorityIndex);
        Assert.All(result.Districts, district =>
        {
            Assert.InRange(district.SanitationCoverageIndex, 0m, 1m);
            Assert.InRange(district.SanitationSupportIndex, 0m, 1m);
            Assert.InRange(district.OverflowRiskIndex, 0m, 1m);
            Assert.InRange(district.ContaminationRiskIndex, 0m, 1m);
            Assert.InRange(district.MaintenancePriorityIndex, 0m, 1m);
        });
    }

    private static GetCityDistrictSanitationConditionsQueryHandler CreateHandler(
        FakeCityEnvironmentalConditionRepository repository,
        FakeCityMapTopologyClient topologyClient)
    {
        return new GetCityDistrictSanitationConditionsQueryHandler(
            repository,
            topologyClient,
            new ClassicCityWeatherPressureProfileFactory(),
            new ClassicCityDistrictSanitationProjectionPolicy());
    }
}
