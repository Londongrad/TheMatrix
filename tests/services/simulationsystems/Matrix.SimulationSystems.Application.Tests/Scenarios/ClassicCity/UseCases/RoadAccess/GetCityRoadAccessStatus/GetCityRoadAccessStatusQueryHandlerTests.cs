using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadAccessStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.RoadAccess.GetCityRoadAccessStatus;

public sealed class GetCityRoadAccessStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new GetCityRoadAccessStatusQueryHandler(
            repository,
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new GetCityRoadAccessStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CreateHostId(), repository.RequestedSimulationHostId);
    }

    [Fact]
    public async Task Handle_WhenStateExists_ReturnsMappedDto()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.ApplyWeatherPressure(new CityWeatherPressureProfile(
            rainPressure: 0.18m,
            snowPressure: 0.34m,
            stormPressure: 0.21m,
            freezePressure: 0.24m,
            thawRelief: 0.04m));
        state.ScheduleRoadAccessMaintenance(
            focus: RoadAccessMaintenanceFocus.CorridorClearance,
            intensity: RoadAccessMaintenanceIntensity.Heavy,
            readyAtTickId: 23);

        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var profileFactory = new ClassicCityWeatherPressureProfileFactory();
        var handler = new GetCityRoadAccessStatusQueryHandler(repository, profileFactory);

        var result = await handler.Handle(
            new GetCityRoadAccessStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(state.FloodingIndex.Value, result.FloodingIndex);
        Assert.Equal(state.RoadAccessibilityIndex.Value, result.RoadAccessibilityIndex);
        Assert.Equal(state.RoadAccessInfrastructure.CorridorAvailabilityIndex, result.CorridorAvailabilityIndex);
        Assert.Equal(state.RoadAccessInfrastructure.SurfaceIntegrityIndex, result.SurfaceIntegrityIndex);
        Assert.Equal(state.RoadAccess.ServiceQualityIndex, result.System.ServiceQualityIndex);
        Assert.Equal(profileFactory.Create(state).RoadSupport, result.RoadSupportIndex);
        Assert.NotNull(result.PendingOperation);
        Assert.Equal("CorridorClearance", result.PendingOperation!.Focus);
        Assert.Equal("Heavy", result.PendingOperation.Intensity);
        Assert.Equal(23, result.PendingOperation.ReadyAtTickId);
    }
}
