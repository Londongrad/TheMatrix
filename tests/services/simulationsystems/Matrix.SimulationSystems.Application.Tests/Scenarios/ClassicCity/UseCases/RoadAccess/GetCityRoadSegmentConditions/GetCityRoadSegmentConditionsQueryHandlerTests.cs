using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadSegmentConditions;

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
        var handler = CreateHandler(repository, topologyClient);

        var result = await handler.Handle(
            new GetCityRoadSegmentConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
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
            new GetCityRoadSegmentConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, topologyClient.RequestedCityId);
        Assert.Equal(1, topologyClient.GetRoadGraphCallCount);
    }

    [Fact]
    public async Task Handle_WhenStateAndTopologyExist_ReturnsProjectedSegmentConditions()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var topologyClient = new FakeCityMapTopologyClient
        {
            Topology = new CityRoadGraphTopologyDto(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                Districts:
                [
                    new CityDistrictTopologyDto(FirstDistrictId, 0m, 0m),
                    new CityDistrictTopologyDto(SecondDistrictId, 18m, 8m)
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
            repository,
            topologyClient,
            profileFactory,
            new ClassicCityRoadSegmentConditionProjectionPolicy());

        var result = await handler.Handle(
            new GetCityRoadSegmentConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastAppliedTickId, result.EffectiveTickId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(profileFactory.Create(state).RoadSupport, result.RoadSupportIndex);
        Assert.Equal(2, result.Segments.Count);
        Assert.Equal(
            [FirstSegmentId, SecondSegmentId],
            result.Segments.Select(x => x.RoadSegmentId).OrderBy(x => x).ToArray());
        Assert.True(result.Segments[0].MaintenancePriorityIndex >= result.Segments[1].MaintenancePriorityIndex);
        Assert.All(result.Segments, segment =>
        {
            Assert.InRange(segment.PassabilityIndex, 0m, 1m);
            Assert.InRange(segment.SpeedMultiplierIndex, 0m, 1.08m);
            Assert.InRange(segment.SlipRiskIndex, 0m, 1m);
            Assert.InRange(segment.ClosureRiskIndex, 0m, 1m);
            Assert.InRange(segment.MaintenancePriorityIndex, 0m, 1m);
        });
    }

    private static GetCityRoadSegmentConditionsQueryHandler CreateHandler(
        FakeCityEnvironmentalConditionRepository repository,
        FakeCityMapTopologyClient topologyClient)
    {
        return new GetCityRoadSegmentConditionsQueryHandler(
            repository,
            topologyClient,
            new ClassicCityWeatherPressureProfileFactory(),
            new ClassicCityRoadSegmentConditionProjectionPolicy());
    }
}
