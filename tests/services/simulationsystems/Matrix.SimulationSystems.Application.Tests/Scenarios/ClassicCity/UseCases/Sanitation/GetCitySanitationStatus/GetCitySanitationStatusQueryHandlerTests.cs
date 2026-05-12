using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.Sanitation.GetCitySanitationStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.Sanitation.GetCitySanitationStatus;

public sealed class GetCitySanitationStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new GetCitySanitationStatusQueryHandler(
            repository,
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new GetCitySanitationStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CreateHostId(), repository.RequestedSimulationHostId);
    }

    [Fact]
    public async Task Handle_WhenStateExists_ReturnsMappedDto()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.ApplyWeatherPressure(new CityWeatherPressureProfile(
            rainPressure: 0.42m,
            snowPressure: 0.04m,
            stormPressure: 0.21m,
            freezePressure: 0.10m,
            thawRelief: 0.06m));
        state.ScheduleSanitationMaintenance(
            focus: SanitationMaintenanceFocus.OverflowControl,
            intensity: SanitationMaintenanceIntensity.Heavy,
            readyAtTickId: 15);

        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var profileFactory = new ClassicCityWeatherPressureProfileFactory();
        var handler = new GetCitySanitationStatusQueryHandler(repository, profileFactory);

        var result = await handler.Handle(
            new GetCitySanitationStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(state.SanitationCoverageIndex.Value, result.SanitationCoverageIndex);
        Assert.Equal(state.SanitationInfrastructure.TreatmentStabilityIndex, result.TreatmentStabilityIndex);
        Assert.Equal(state.SanitationInfrastructure.OverflowControlIndex, result.OverflowControlIndex);
        Assert.Equal(state.Sanitation.ServiceQualityIndex, result.System.ServiceQualityIndex);
        Assert.Equal(profileFactory.Create(state).SanitationSupport, result.SanitationSupportIndex);
        Assert.NotNull(result.PendingOperation);
        Assert.Equal("OverflowControl", result.PendingOperation!.Focus);
        Assert.Equal("Heavy", result.PendingOperation.Intensity);
        Assert.Equal(15, result.PendingOperation.ReadyAtTickId);
    }
}
