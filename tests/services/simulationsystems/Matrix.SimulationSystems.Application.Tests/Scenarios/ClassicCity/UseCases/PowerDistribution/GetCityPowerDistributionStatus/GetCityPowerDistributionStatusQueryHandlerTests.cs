using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.PowerDistribution.GetCityPowerDistributionStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.PowerDistribution.GetCityPowerDistributionStatus;

public sealed class GetCityPowerDistributionStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new GetCityPowerDistributionStatusQueryHandler(
            repository,
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new GetCityPowerDistributionStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
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
            snowPressure: 0.14m,
            stormPressure: 0.33m,
            freezePressure: 0.11m,
            thawRelief: 0.07m));
        state.SchedulePowerDistributionMaintenance(
            focus: PowerDistributionMaintenanceFocus.SwitchingRecovery,
            intensity: PowerDistributionMaintenanceIntensity.Heavy,
            readyAtTickId: 17);

        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var profileFactory = new ClassicCityWeatherPressureProfileFactory();
        var handler = new GetCityPowerDistributionStatusQueryHandler(repository, profileFactory);

        var result = await handler.Handle(
            new GetCityPowerDistributionStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(state.PowerCoverageIndex.Value, result.PowerCoverageIndex);
        Assert.Equal(state.PowerDistributionInfrastructure.SubstationCapacityIndex, result.SubstationCapacityIndex);
        Assert.Equal(state.PowerDistributionInfrastructure.GridIntegrityIndex, result.GridIntegrityIndex);
        Assert.Equal(state.PowerDistribution.ServiceQualityIndex, result.System.ServiceQualityIndex);
        Assert.Equal(profileFactory.Create(state).PowerSupport, result.PowerSupportIndex);
        Assert.NotNull(result.PendingOperation);
        Assert.Equal("SwitchingRecovery", result.PendingOperation!.Focus);
        Assert.Equal("Heavy", result.PendingOperation.Intensity);
        Assert.Equal(17, result.PendingOperation.ReadyAtTickId);
    }
}
