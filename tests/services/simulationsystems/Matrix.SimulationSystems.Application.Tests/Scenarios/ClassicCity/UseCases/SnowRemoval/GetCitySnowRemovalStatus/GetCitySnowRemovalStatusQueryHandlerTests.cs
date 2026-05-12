using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.SnowRemoval.GetCitySnowRemovalStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.SnowRemoval.GetCitySnowRemovalStatus;

public sealed class GetCitySnowRemovalStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new GetCitySnowRemovalStatusQueryHandler(
            repository,
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new GetCitySnowRemovalStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CreateHostId(), repository.RequestedSimulationHostId);
    }

    [Fact]
    public async Task Handle_WhenStateExists_ReturnsMappedDto()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.ApplyWeatherPressure(new CityWeatherPressureProfile(
            rainPressure: 0.04m,
            snowPressure: 0.42m,
            stormPressure: 0.18m,
            freezePressure: 0.29m,
            thawRelief: 0.03m));
        state.ScheduleSnowRemovalMaintenance(
            focus: SnowRemovalMaintenanceFocus.RouteClearance,
            intensity: SnowRemovalMaintenanceIntensity.Heavy,
            readyAtTickId: 19);

        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var profileFactory = new ClassicCityWeatherPressureProfileFactory();
        var handler = new GetCitySnowRemovalStatusQueryHandler(repository, profileFactory);

        var result = await handler.Handle(
            new GetCitySnowRemovalStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(state.SnowAccumulationIndex.Value, result.SnowAccumulationIndex);
        Assert.Equal(state.SnowRemovalInfrastructure.FleetAvailabilityIndex, result.FleetAvailabilityIndex);
        Assert.Equal(state.SnowRemovalInfrastructure.RouteCoverageIndex, result.RouteCoverageIndex);
        Assert.Equal(state.SnowRemoval.ServiceQualityIndex, result.System.ServiceQualityIndex);
        Assert.Equal(profileFactory.Create(state).SnowRemovalSupport, result.SnowRemovalSupportIndex);
        Assert.NotNull(result.PendingOperation);
        Assert.Equal("RouteClearance", result.PendingOperation!.Focus);
        Assert.Equal("Heavy", result.PendingOperation.Intensity);
        Assert.Equal(19, result.PendingOperation.ReadyAtTickId);
    }
}
