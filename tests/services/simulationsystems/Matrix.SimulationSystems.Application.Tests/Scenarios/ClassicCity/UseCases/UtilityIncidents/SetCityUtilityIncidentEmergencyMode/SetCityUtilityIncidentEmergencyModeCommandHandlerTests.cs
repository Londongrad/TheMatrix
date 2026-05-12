using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.SetCityUtilityIncidentEmergencyMode;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.UtilityIncidents.SetCityUtilityIncidentEmergencyMode;

public sealed class SetCityUtilityIncidentEmergencyModeCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new SetCityUtilityIncidentEmergencyModeCommandHandler(
            repository,
            new FakeUnitOfWork(),
            new CityEnvironmentalConditionPolicy(),
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new SetCityUtilityIncidentEmergencyModeCommand(
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
        var handler = new SetCityUtilityIncidentEmergencyModeCommandHandler(
            repository,
            unitOfWork,
            new CityEnvironmentalConditionPolicy(),
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new SetCityUtilityIncidentEmergencyModeCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                Enabled: true),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(state.UtilityIncidentInfrastructure.EmergencyModeEnabled);
        Assert.True(result!.EmergencyModeEnabled);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(state.UtilityIncidentInfrastructure.DispatchReadinessIndex, result.DispatchReadinessIndex);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
