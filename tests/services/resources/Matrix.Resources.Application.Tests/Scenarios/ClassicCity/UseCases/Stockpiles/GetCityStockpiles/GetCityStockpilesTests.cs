using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.GetCityStockpiles;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common;
using Xunit;
using Matrix.Resources.Application.Tests.TestSupport;
using static Matrix.Resources.Application.Tests.TestSupport.ResourcesApplicationTestSupport;

namespace Matrix.Resources.Application.Tests.Scenarios.ClassicCity.UseCases.Stockpiles.GetCityStockpiles;

public sealed class GetCityStockpilesTests
{
    [Fact]
    public void Validator_RejectsEmptyCityId()
    {
        var validator = new GetCityStockpilesQueryValidator();

        var result = validator.Validate(new GetCityStockpilesQuery(Guid.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Handler_ReturnsNullWhenStateIsMissing()
    {
        var repository = new FakeCityStockpileRepository();
        var handler = new GetCityStockpilesQueryHandler(repository);

        CityStockpilesDto? dto = await handler.Handle(new GetCityStockpilesQuery(CityId), CancellationToken.None);

        Assert.Null(dto);
        Assert.Equal(CreateHostId(), repository.RequestedSimulationHostId);
    }

    [Fact]
    public async Task Handler_MapsDomainStateToDto()
    {
        var repository = new FakeCityStockpileRepository
        {
            State = CreateState()
        };
        repository.State.MarkTickApplied(9);
        var handler = new GetCityStockpilesQueryHandler(repository);

        CityStockpilesDto? dto = await handler.Handle(new GetCityStockpilesQuery(CityId), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(CityId, dto.CityId);
        Assert.Equal(9, dto.EffectiveTickId);
        Assert.Equal(repository.State.SupplyStressIndex, dto.SupplyStressIndex);
        Assert.Equal(repository.State.Fuel.StockLevelIndex, dto.Fuel.StockLevelIndex);
    }
}
