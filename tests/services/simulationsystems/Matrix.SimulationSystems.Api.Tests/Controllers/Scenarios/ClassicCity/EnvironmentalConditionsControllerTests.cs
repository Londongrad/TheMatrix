using Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.GetCityEnvironmentalConditions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationSystems.Api.Tests.TestSupport.SimulationSystemsApiTestSupport;

namespace Matrix.SimulationSystems.Api.Tests.Controllers.Scenarios.ClassicCity;

public sealed class EnvironmentalConditionsControllerTests
{
    [Fact]
    public async Task Get_WhenConditionsAreMissing_ReturnsNotFound()
    {
        Guid cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
        var sender = new FakeSender();
        sender.Handle<GetCityEnvironmentalConditionsQuery, CityEnvironmentalConditionsDto?>(_ => null);
        var controller = new EnvironmentalConditionsController(sender);

        IResult result = await controller.Get(cityId, CancellationToken.None);

        AssertStatus(result, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Get_WhenConditionsExist_ReturnsMappedView()
    {
        Guid cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
        var sender = new FakeSender();
        sender.Handle<GetCityEnvironmentalConditionsQuery, CityEnvironmentalConditionsDto?>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return CreateEnvironmentalConditionsDto(cityId);
        });
        var controller = new EnvironmentalConditionsController(sender);

        IResult result = await controller.Get(cityId, CancellationToken.None);

        CityEnvironmentalConditionsView view = AssertResult<CityEnvironmentalConditionsView>(result, StatusCodes.Status200OK);
        Assert.Equal(cityId, view.CityId);
        Assert.Equal("SystemsDegradation", view.EffectivePhase);
        Assert.Equal(0.72m, view.ResourceSupply.Fuel.StockLevelIndex);
        Assert.Equal("Drainage", view.Drainage.Kind);
        Assert.Equal(0.14m, view.Sanitation.BacklogIndex);
    }
}
