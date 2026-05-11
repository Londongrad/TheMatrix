using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Heating.GetCityHeatingStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Heating.GetCityHeatingStatus;

public sealed class GetCityHeatingStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new GetCityHeatingStatusQueryHandler(
            repository,
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new GetCityHeatingStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CreateHostId(), repository.RequestedSimulationHostId);
    }

    [Fact]
    public async Task Handle_WhenStateExists_ReturnsMappedDto()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.ApplyWeatherPressure(new CityWeatherPressureProfile(
            rainPressure: 0.19m,
            snowPressure: 0.22m,
            stormPressure: 0.14m,
            freezePressure: 0.47m,
            thawRelief: 0.03m));
        state.ScheduleHeatingMaintenance(
            focus: HeatingMaintenanceFocus.PlantRepairs,
            intensity: HeatingMaintenanceIntensity.Heavy,
            readyAtTickId: 11);

        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var profileFactory = new ClassicCityWeatherPressureProfileFactory();
        var handler = new GetCityHeatingStatusQueryHandler(repository, profileFactory);

        var result = await handler.Handle(
            new GetCityHeatingStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(state.HeatingCoverageIndex.Value, result.HeatingCoverageIndex);
        Assert.Equal(state.HeatingInfrastructure.PlantCapacityIndex, result.PlantCapacityIndex);
        Assert.Equal(state.HeatingInfrastructure.ControlReadinessIndex, result.ControlReadinessIndex);
        Assert.Equal(state.Heating.ServiceQualityIndex, result.System.ServiceQualityIndex);
        Assert.Equal(profileFactory.Create(state).HeatingSupport, result.HeatingSupportIndex);
        Assert.NotNull(result.PendingOperation);
        Assert.Equal("PlantRepairs", result.PendingOperation!.Focus);
        Assert.Equal("Heavy", result.PendingOperation.Intensity);
        Assert.Equal(11, result.PendingOperation.ReadyAtTickId);
    }
}
