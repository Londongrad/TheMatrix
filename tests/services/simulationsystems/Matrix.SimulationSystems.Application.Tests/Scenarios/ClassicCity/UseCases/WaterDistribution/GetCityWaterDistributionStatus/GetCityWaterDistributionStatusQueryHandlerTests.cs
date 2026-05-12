using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.WaterDistribution.GetCityWaterDistributionStatus;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.WaterDistribution.GetCityWaterDistributionStatus;

public sealed class GetCityWaterDistributionStatusQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new GetCityWaterDistributionStatusQueryHandler(
            repository,
            new ClassicCityWeatherPressureProfileFactory());

        var result = await handler.Handle(
            new GetCityWaterDistributionStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CreateHostId(), repository.RequestedSimulationHostId);
    }

    [Fact]
    public async Task Handle_WhenStateExists_ReturnsMappedDto()
    {
        var state = SimulationSystemsApplicationTestSupport.CreateState();
        state.ApplyWeatherPressure(new CityWeatherPressureProfile(
            rainPressure: 0.31m,
            snowPressure: 0.08m,
            stormPressure: 0.27m,
            freezePressure: 0.16m,
            thawRelief: 0.10m));
        state.ScheduleWaterDistributionMaintenance(
            focus: WaterDistributionMaintenanceFocus.PumpRecovery,
            intensity: WaterDistributionMaintenanceIntensity.Heavy,
            readyAtTickId: 13);

        var repository = new FakeCityEnvironmentalConditionRepository { State = state };
        var profileFactory = new ClassicCityWeatherPressureProfileFactory();
        var handler = new GetCityWaterDistributionStatusQueryHandler(repository, profileFactory);

        var result = await handler.Handle(
            new GetCityWaterDistributionStatusQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(state.LastEvaluatedAtUtc, result.LastEvaluatedAtUtc);
        Assert.Equal(state.WaterCoverageIndex.Value, result.WaterCoverageIndex);
        Assert.Equal(state.WaterDistributionInfrastructure.TreatmentCapacityIndex, result.TreatmentCapacityIndex);
        Assert.Equal(state.WaterDistributionInfrastructure.PumpReadinessIndex, result.PumpReadinessIndex);
        Assert.Equal(state.WaterDistribution.ServiceQualityIndex, result.System.ServiceQualityIndex);
        Assert.Equal(profileFactory.Create(state).WaterSupport, result.WaterSupportIndex);
        Assert.NotNull(result.PendingOperation);
        Assert.Equal("PumpRecovery", result.PendingOperation!.Focus);
        Assert.Equal("Heavy", result.PendingOperation.Intensity);
        Assert.Equal(13, result.PendingOperation.ReadyAtTickId);
    }
}
