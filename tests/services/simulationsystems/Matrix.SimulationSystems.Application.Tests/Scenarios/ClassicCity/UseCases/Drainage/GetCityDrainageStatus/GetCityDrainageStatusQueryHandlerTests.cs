using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.GetCityDrainageStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Drainage.GetCityDrainageStatus;

public sealed class GetCityDrainageStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new GetCityDrainageStatusQueryHandler(
            repository,
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new GetCityDrainageStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CreateHostId(), repository.RequestedSimulationHostId);
    }

    [Fact]
    public async Task Handle_WhenStateExists_ReturnsMappedDto()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.ApplyWeatherPressure(new CityWeatherPressureProfile(
            rainPressure: 0.36m,
            snowPressure: 0.18m,
            stormPressure: 0.44m,
            freezePressure: 0.11m,
            thawRelief: 0.05m));
        state.ScheduleDrainageMaintenance(
            focus: DrainageMaintenanceFocus.PumpRepairs,
            intensity: DrainageMaintenanceIntensity.Heavy,
            readyAtTickId: 9);

        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var profileFactory = new ClassicCityWeatherPressureProfileFactory();
        var handler = new GetCityDrainageStatusQueryHandler(repository, profileFactory);

        var result = await handler.Handle(
            new GetCityDrainageStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(state.FloodingIndex.Value, result.FloodingIndex);
        Assert.Equal(state.DrainageInfrastructure.PumpCapacityIndex, result.PumpCapacityIndex);
        Assert.Equal(state.DrainageInfrastructure.NetworkIntegrityIndex, result.NetworkIntegrityIndex);
        Assert.Equal(state.Drainage.LoadIndex, result.System.LoadIndex);
        Assert.Equal(profileFactory.Create(state).DrainageSupport, result.DrainageSupportIndex);
        Assert.NotNull(result.PendingOperation);
        Assert.Equal("PumpRepairs", result.PendingOperation!.Focus);
        Assert.Equal("Heavy", result.PendingOperation.Intensity);
        Assert.Equal(9, result.PendingOperation.ReadyAtTickId);
    }
}
