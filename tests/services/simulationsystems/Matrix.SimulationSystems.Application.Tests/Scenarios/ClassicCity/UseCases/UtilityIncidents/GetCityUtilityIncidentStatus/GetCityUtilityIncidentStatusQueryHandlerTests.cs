using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityUtilityIncidentStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.UtilityIncidents.GetCityUtilityIncidentStatus;

public sealed class GetCityUtilityIncidentStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new GetCityUtilityIncidentStatusQueryHandler(
            repository,
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new GetCityUtilityIncidentStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CreateHostId(), repository.RequestedSimulationHostId);
    }

    [Fact]
    public async Task Handle_WhenStateExists_ReturnsMappedDto()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.ApplyWeatherPressure(new CityWeatherPressureProfile(
            rainPressure: 0.23m,
            snowPressure: 0.17m,
            stormPressure: 0.35m,
            freezePressure: 0.14m,
            thawRelief: 0.05m));
        state.ScheduleUtilityIncidentResponse(
            focus: UtilityIncidentResponseFocus.PowerOutages,
            intensity: UtilityIncidentResponseIntensity.Heavy,
            focusDistrictId: Guid.Parse("74000000-0000-0000-0000-000000000001"),
            readyAtTickId: 21);

        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var profileFactory = new ClassicCityWeatherPressureProfileFactory();
        var handler = new GetCityUtilityIncidentStatusQueryHandler(repository, profileFactory);

        var result = await handler.Handle(
            new GetCityUtilityIncidentStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(state.UtilityContinuityIndex.Value, result.UtilityContinuityIndex);
        Assert.Equal(state.UtilityIncidentInfrastructure.DispatchReadinessIndex, result.DispatchReadinessIndex);
        Assert.Equal(state.UtilityIncidentInfrastructure.RestorationCoverageIndex, result.RestorationCoverageIndex);
        Assert.Equal(state.UtilityIncidents.ServiceQualityIndex, result.System.ServiceQualityIndex);
        Assert.Equal(profileFactory.Create(state).UtilityIncidentSupport, result.UtilityIncidentSupportIndex);
        Assert.NotNull(result.PendingOperation);
        Assert.Equal("PowerOutages", result.PendingOperation!.Focus);
        Assert.Equal("Heavy", result.PendingOperation.Intensity);
        Assert.Equal(21, result.PendingOperation.ReadyAtTickId);
        Assert.Equal(Guid.Parse("74000000-0000-0000-0000-000000000001"), result.FocusDistrictId);
    }
}
