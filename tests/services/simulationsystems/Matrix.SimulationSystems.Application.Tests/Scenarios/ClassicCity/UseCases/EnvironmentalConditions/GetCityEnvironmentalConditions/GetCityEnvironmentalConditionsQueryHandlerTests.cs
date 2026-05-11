using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.GetCityEnvironmentalConditions;
using Matrix.SimulationSystems.Application.Tests.TestSupport;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models;
using Xunit;

namespace Matrix.SimulationSystems.Application.Tests.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.GetCityEnvironmentalConditions;

public sealed class GetCityEnvironmentalConditionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenStateDoesNotExist_ReturnsNull()
    {
        var repository = new FakeCityEnvironmentalConditionRepository();
        var handler = new GetCityEnvironmentalConditionsQueryHandler(repository);

        CityEnvironmentalConditionsDto? result = await handler.Handle(
            new GetCityEnvironmentalConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CreateHostId(), repository.RequestedSimulationHostId);
    }

    [Fact]
    public async Task Handle_WhenStateExists_ReturnsMappedDto()
    {
        var repository = new FakeCityEnvironmentalConditionRepository
        {
            State = SimulationSystemsApplicationTestSupport.CreateState()
        };
        repository.State!.ApplyWeatherPressure(new CityWeatherPressureProfile(
            rainPressure: 0.35m,
            snowPressure: 0.16m,
            stormPressure: 0.42m,
            freezePressure: 0.09m,
            thawRelief: 0.11m));
        repository.State.ApplyResourceSupply(new CityResourceSupplySnapshot(
            supplyStressIndex: 0.44m,
            fuelStockLevelIndex: 0.55m,
            fuelResupplyReadinessIndex: 0.61m,
            fuelShortageRiskIndex: 0.27m,
            sparePartsStockLevelIndex: 0.51m,
            sparePartsResupplyReadinessIndex: 0.58m,
            sparePartsShortageRiskIndex: 0.32m,
            filtersStockLevelIndex: 0.49m,
            filtersResupplyReadinessIndex: 0.63m,
            filtersShortageRiskIndex: 0.29m,
            emergencyWaterStockLevelIndex: 0.77m,
            emergencyWaterResupplyReadinessIndex: 0.68m,
            emergencyWaterShortageRiskIndex: 0.18m,
            effectiveTickId: 4,
            effectiveAtUtc: SimulationSystemsApplicationTestSupport.LaterUtc));
        repository.State.MarkTickApplied(4);

        var handler = new GetCityEnvironmentalConditionsQueryHandler(repository);

        CityEnvironmentalConditionsDto? result = await handler.Handle(
            new GetCityEnvironmentalConditionsQuery(SimulationSystemsApplicationTestSupport.CityId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(SimulationSystemsApplicationTestSupport.CityId, result!.CityId);
        Assert.Equal(4, result.EffectiveTickId);
        Assert.Equal(repository.State.FloodingIndex.Value, result.FloodingIndex);
        Assert.Equal(repository.State.Drainage.LoadIndex, result.Drainage.LoadIndex);
        Assert.Equal(repository.State.PowerDistribution.ServiceQualityIndex, result.PowerDistribution.ServiceQualityIndex);
        Assert.Equal(0.44m, result.ResourceSupply.SupplyStressIndex);
        Assert.Equal(0.55m, result.ResourceSupply.Fuel.StockLevelIndex);
        Assert.Equal(SimulationSystemsApplicationTestSupport.LaterUtc, result.ResourceSupply.EffectiveAtUtc);
    }
}
