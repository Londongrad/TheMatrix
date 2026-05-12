using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.SetCitySanitationEmergencyMode;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Services;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Sanitation.SetCitySanitationEmergencyMode;

public sealed class SetCitySanitationEmergencyModeCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new SetCitySanitationEmergencyModeCommandHandler(
            repository,
            new FakeUnitOfWork(),
            new CityEnvironmentalConditionPolicy(),
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new SetCitySanitationEmergencyModeCommand(
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
        var handler = new SetCitySanitationEmergencyModeCommandHandler(
            repository,
            unitOfWork,
            new CityEnvironmentalConditionPolicy(),
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new SetCitySanitationEmergencyModeCommand(
                CityId: SimulationSystemsApplicationTestSupport.CityId,
                Enabled: true),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(state.SanitationInfrastructure.EmergencyModeEnabled);
        Assert.True(result!.EmergencyModeEnabled);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(state.SanitationInfrastructure.TreatmentStabilityIndex, result.TreatmentStabilityIndex);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
