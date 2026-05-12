using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Heating.GetCityDistrictHeatingConditions;

public sealed class GetCityDistrictHeatingConditionsQueryHandlerTests
{
    private static readonly Guid FirstDistrictId = Guid.Parse("77000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondDistrictId = Guid.Parse("77000000-0000-0000-0000-000000000002");

    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var topologyClient = new FakeCityMapTopologyClient();
        var handler = CreateHandler(repository, topologyClient);

        var result = await handler.Handle(
            new GetCityDistrictHeatingConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
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
            new GetCityDistrictHeatingConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
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
                new CityDistrictTopologyDto(SecondDistrictId, 20m, 10m))
        };
        var profileFactory = new ClassicCityWeatherPressureProfileFactory();
        var handler = new GetCityDistrictHeatingConditionsQueryHandler(
            repository,
            topologyClient,
            profileFactory,
            new ClassicCityDistrictHeatingProjectionPolicy());

        var result = await handler.Handle(
            new GetCityDistrictHeatingConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastAppliedTickId, result.EffectiveTickId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(profileFactory.Create(state).HeatingSupport, result.HeatingSupportIndex);
        Assert.Equal(2, result.Districts.Count);
        Assert.Equal(
            [FirstDistrictId, SecondDistrictId],
            result.Districts.Select(x => x.DistrictId).OrderBy(x => x).ToArray());
        Assert.True(result.Districts[0].MaintenancePriorityIndex >= result.Districts[1].MaintenancePriorityIndex);
        Assert.All(result.Districts, district =>
        {
            Assert.InRange(district.HeatingCoverageIndex, 0m, 1m);
            Assert.InRange(district.HeatingSupportIndex, 0m, 1m);
            Assert.InRange(district.OutageRiskIndex, 0m, 1m);
            Assert.InRange(district.ComfortStressIndex, 0m, 1m);
            Assert.InRange(district.MaintenancePriorityIndex, 0m, 1m);
        });
    }

    private static GetCityDistrictHeatingConditionsQueryHandler CreateHandler(
        FakeCityEnvironmentalConditionRepository repository,
        FakeCityMapTopologyClient topologyClient)
    {
        return new GetCityDistrictHeatingConditionsQueryHandler(
            repository,
            topologyClient,
            new ClassicCityWeatherPressureProfileFactory(),
            new ClassicCityDistrictHeatingProjectionPolicy());
    }
}
