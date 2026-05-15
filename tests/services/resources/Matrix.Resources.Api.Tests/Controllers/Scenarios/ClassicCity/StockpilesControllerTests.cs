using Matrix.Resources.Api.Controllers.Scenarios.ClassicCity;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.Common;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.GetCityStockpiles;
using Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SetCityEmergencyRationing;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Requests;
using Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.Resources.Api.Tests.TestSupport.ResourcesApiTestSupport;
using DomainResupplyFocus = Matrix.Resources.Domain.Scenarios.ClassicCity.Enums.ResupplyFocus;
using DomainResupplyIntensity = Matrix.Resources.Domain.Scenarios.ClassicCity.Enums.ResupplyIntensity;
using RequestResupplyFocus = Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Requests.ResupplyFocus;
using RequestResupplyIntensity = Matrix.Resources.Contracts.Scenarios.ClassicCity.Stockpiles.Requests.ResupplyIntensity;

namespace Matrix.Resources.Api.Tests.Controllers.Scenarios.ClassicCity;

public sealed class StockpilesControllerTests
{
    [Fact]
    public async Task Get_WhenStockpilesAreMissing_ReturnsNotFound()
    {
        Guid cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
        var sender = new FakeSender();
        sender.Handle<GetCityStockpilesQuery, CityStockpilesDto?>(_ => null);
        var controller = new StockpilesController(sender);

        IResult result = await controller.Get(cityId, CancellationToken.None);

        AssertStatus(result, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Get_WhenStockpilesExist_ReturnsMappedView()
    {
        Guid cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
        var sender = new FakeSender();
        sender.Handle<GetCityStockpilesQuery, CityStockpilesDto?>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return CreateCityStockpilesDto(cityId);
        });
        var controller = new StockpilesController(sender);

        IResult result = await controller.Get(cityId, CancellationToken.None);

        CityStockpilesView view = AssertResult<CityStockpilesView>(result, StatusCodes.Status200OK);
        Assert.Equal(cityId, view.CityId);
        Assert.Equal("ResourceSettlement", view.EffectivePhase);
        Assert.Equal("Fuel", view.Fuel.Kind);
        Assert.Equal(0.22m, view.Fuel.ShortageRiskIndex);
        Assert.Equal("All", view.PendingResupply!.Focus);
    }

    [Fact]
    public async Task SetEmergencyRationing_WhenNotInitialized_ReturnsNotFound()
    {
        Guid cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
        var sender = new FakeSender();
        sender.Handle<SetCityEmergencyRationingCommand, SetCityEmergencyRationingResult>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.True(command.Enabled);
            return new SetCityEmergencyRationingResult(
                Status: SetCityEmergencyRationingStatus.NotInitialized,
                CityId: cityId,
                EmergencyRationingEnabled: false,
                SupplyStressIndex: 0m);
        });
        var controller = new StockpilesController(sender);

        IResult result = await controller.SetEmergencyRationing(
            cityId,
            new SetCityEmergencyRationingRequest(true),
            CancellationToken.None);

        AssertStatus(result, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task SetEmergencyRationing_WhenApplied_ReloadsCurrentView()
    {
        Guid cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
        var sender = new FakeSender();
        sender.Handle<SetCityEmergencyRationingCommand, SetCityEmergencyRationingResult>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.True(command.Enabled);
            return new SetCityEmergencyRationingResult(
                Status: SetCityEmergencyRationingStatus.Applied,
                CityId: cityId,
                EmergencyRationingEnabled: true,
                SupplyStressIndex: 0.37m);
        });
        sender.Handle<GetCityStockpilesQuery, CityStockpilesDto?>(query =>
        {
            Assert.Equal(cityId, query.CityId);
            return CreateCityStockpilesDto(cityId, emergencyRationingEnabled: true);
        });
        var controller = new StockpilesController(sender);

        IResult result = await controller.SetEmergencyRationing(
            cityId,
            new SetCityEmergencyRationingRequest(true),
            CancellationToken.None);

        CityStockpilesView view = AssertResult<CityStockpilesView>(result, StatusCodes.Status200OK);
        Assert.True(view.EmergencyRationingEnabled);
        Assert.Collection(
            sender.Requests,
            request => Assert.IsType<SetCityEmergencyRationingCommand>(request),
            request => Assert.IsType<GetCityStockpilesQuery>(request));
    }

    [Fact]
    public async Task DispatchResupply_WhenNotInitialized_ReturnsNotFound()
    {
        Guid cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
        var sender = new FakeSender();
        sender.Handle<DispatchCityResupplyCommand, DispatchCityResupplyResult>(command =>
        {
            Assert.Equal(cityId, command.CityId);
            Assert.Equal(DomainResupplyFocus.All, command.Focus);
            Assert.Equal(DomainResupplyIntensity.Medium, command.Intensity);
            Assert.False(command.EmergencyOverride);
            Assert.Null(command.FocusDistrictId);
            return CreateDispatchCityResupplyResult(DispatchCityResupplyStatus.NotInitialized, cityId);
        });
        var controller = new StockpilesController(sender);

        IResult result = await controller.DispatchResupply(
            cityId,
            new DispatchCityResupplyRequest(),
            CancellationToken.None);

        AssertStatus(result, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task DispatchResupply_WhenAuthorizationIsBlocked_ReturnsConflictView()
    {
        Guid cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
        Guid districtId = Guid.Parse("28e9e9cc-f6fc-4955-a733-c938586b4858");
        var sender = new FakeSender();
        sender.Handle<DispatchCityResupplyCommand, DispatchCityResupplyResult>(command =>
        {
            Assert.Equal(DomainResupplyFocus.Medicine, command.Focus);
            Assert.Equal(DomainResupplyIntensity.High, command.Intensity);
            Assert.True(command.EmergencyOverride);
            Assert.Equal(districtId, command.FocusDistrictId);
            return CreateDispatchCityResupplyResult(DispatchCityResupplyStatus.AuthorizationDenied, cityId);
        });
        var controller = new StockpilesController(sender);

        IResult result = await controller.DispatchResupply(
            cityId,
            new DispatchCityResupplyRequest(RequestResupplyFocus.Medicine, RequestResupplyIntensity.High, districtId, true),
            CancellationToken.None);

        DispatchCityResupplyView view = AssertResult<DispatchCityResupplyView>(result, StatusCodes.Status409Conflict);
        Assert.Equal("AuthorizationDenied", view.Status);
        Assert.Equal("Denied", view.BudgetAuthorizationStatus);
        Assert.Equal("Medium", view.AppliedIntensity);
    }

    [Fact]
    public async Task DispatchResupply_WhenEnumsAreOutOfRange_UsesFallbackMappings()
    {
        Guid cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
        var sender = new FakeSender();
        sender.Handle<DispatchCityResupplyCommand, DispatchCityResupplyResult>(command =>
        {
            Assert.Equal(DomainResupplyFocus.All, command.Focus);
            Assert.Equal(DomainResupplyIntensity.Medium, command.Intensity);
            return CreateDispatchCityResupplyResult(DispatchCityResupplyStatus.Scheduled, cityId);
        });
        var controller = new StockpilesController(sender);

        IResult result = await controller.DispatchResupply(
            cityId,
            new DispatchCityResupplyRequest((RequestResupplyFocus)999, (RequestResupplyIntensity)999, null, false),
            CancellationToken.None);

        DispatchCityResupplyView view = AssertResult<DispatchCityResupplyView>(result, StatusCodes.Status200OK);
        Assert.Equal("Scheduled", view.Status);
        Assert.Equal("Managed", view.BudgetAuthorizationLevel);
        Assert.Equal(0.67m, view.EmergencyWaterStockLevelIndex);
    }
}
