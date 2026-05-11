using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Drainage.SetCityDrainageEmergencyMode;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Drainage.SetCityDrainageEmergencyMode;

public sealed class SetCityDrainageEmergencyModeCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new SetCityDrainageEmergencyModeCommandHandler(
            repository,
            new FakeUnitOfWork(),
            new CityEnvironmentalConditionPolicy(),
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new SetCityDrainageEmergencyModeCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                Enabled: true),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CreateHostId(), repository.RequestedSimulationHostId);
    }

    [Fact]
    public async Task Handle_WhenStateExists_TogglesEmergencyModeAndReturnsUpdatedDto()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SetCityDrainageEmergencyModeCommandHandler(
            repository,
            unitOfWork,
            new CityEnvironmentalConditionPolicy(),
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new SetCityDrainageEmergencyModeCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                Enabled: true),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(state.DrainageInfrastructure.EmergencyModeEnabled);
        Assert.True(result!.EmergencyModeEnabled);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(state.DrainageInfrastructure.PumpCapacityIndex, result.PumpCapacityIndex);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
