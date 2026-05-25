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

namespace Matrix.Resources.Api.Tests.Controllers.Scenarios.ClassicCity
{
    public sealed class StockpilesControllerTests
    {
        [Fact]
        public async Task Get_WhenStockpilesAreMissing_ReturnsNotFound()
        {
            var cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
            var sender = new FakeSender();
            sender.Handle<GetCityStockpilesQuery, CityStockpilesDto?>(_ => null);
            var controller = new StockpilesController(sender);

            IResult result = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            AssertStatus(
                result: result,
                expectedStatusCode: StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task Get_WhenStockpilesExist_ReturnsMappedView()
        {
            var cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
            var sender = new FakeSender();
            sender.Handle<GetCityStockpilesQuery, CityStockpilesDto?>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return CreateCityStockpilesDto(cityId);
            });
            var controller = new StockpilesController(sender);

            IResult result = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            CityStockpilesView view = AssertResult<CityStockpilesView>(
                result: result,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Equal(
                expected: cityId,
                actual: view.CityId);
            Assert.Equal(
                expected: "ResourceSettlement",
                actual: view.EffectivePhase);
            Assert.Equal(
                expected: "Fuel",
                actual: view.Fuel.Kind);
            Assert.Equal(
                expected: 0.22m,
                actual: view.Fuel.ShortageRiskIndex);
            Assert.Equal(
                expected: "All",
                actual: view.PendingResupply!.Focus);
        }

        [Fact]
        public async Task SetEmergencyRationing_WhenNotInitialized_ReturnsNotFound()
        {
            var cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
            var sender = new FakeSender();
            sender.Handle<SetCityEmergencyRationingCommand, SetCityEmergencyRationingResult>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.True(command.Enabled);
                return new SetCityEmergencyRationingResult(
                    Status: SetCityEmergencyRationingStatus.NotInitialized,
                    CityId: cityId,
                    EmergencyRationingEnabled: false,
                    SupplyStressIndex: 0m);
            });
            var controller = new StockpilesController(sender);

            IResult result = await controller.SetEmergencyRationing(
                cityId: cityId,
                request: new SetCityEmergencyRationingRequest(true),
                cancellationToken: CancellationToken.None);

            AssertStatus(
                result: result,
                expectedStatusCode: StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task SetEmergencyRationing_WhenApplied_ReloadsCurrentView()
        {
            var cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
            var sender = new FakeSender();
            sender.Handle<SetCityEmergencyRationingCommand, SetCityEmergencyRationingResult>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.True(command.Enabled);
                return new SetCityEmergencyRationingResult(
                    Status: SetCityEmergencyRationingStatus.Applied,
                    CityId: cityId,
                    EmergencyRationingEnabled: true,
                    SupplyStressIndex: 0.37m);
            });
            sender.Handle<GetCityStockpilesQuery, CityStockpilesDto?>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return CreateCityStockpilesDto(
                    cityId: cityId,
                    emergencyRationingEnabled: true);
            });
            var controller = new StockpilesController(sender);

            IResult result = await controller.SetEmergencyRationing(
                cityId: cityId,
                request: new SetCityEmergencyRationingRequest(true),
                cancellationToken: CancellationToken.None);

            CityStockpilesView view = AssertResult<CityStockpilesView>(
                result: result,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.True(view.EmergencyRationingEnabled);
            Assert.Collection(
                collection: sender.Requests,
                request => Assert.IsType<SetCityEmergencyRationingCommand>(request),
                request => Assert.IsType<GetCityStockpilesQuery>(request));
        }

        [Fact]
        public async Task DispatchResupply_WhenNotInitialized_ReturnsNotFound()
        {
            var cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
            var sender = new FakeSender();
            sender.Handle<DispatchCityResupplyCommand, DispatchCityResupplyResult>(command =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: command.CityId);
                Assert.Equal(
                    expected: DomainResupplyFocus.All,
                    actual: command.Focus);
                Assert.Equal(
                    expected: DomainResupplyIntensity.Medium,
                    actual: command.Intensity);
                Assert.False(command.EmergencyOverride);
                Assert.Null(command.FocusDistrictId);
                return CreateDispatchCityResupplyResult(
                    status: DispatchCityResupplyStatus.NotInitialized,
                    cityId: cityId);
            });
            var controller = new StockpilesController(sender);

            IResult result = await controller.DispatchResupply(
                cityId: cityId,
                request: new DispatchCityResupplyRequest(),
                cancellationToken: CancellationToken.None);

            AssertStatus(
                result: result,
                expectedStatusCode: StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task DispatchResupply_WhenAuthorizationIsBlocked_ReturnsConflictView()
        {
            var cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
            var districtId = Guid.Parse("28e9e9cc-f6fc-4955-a733-c938586b4858");
            var sender = new FakeSender();
            sender.Handle<DispatchCityResupplyCommand, DispatchCityResupplyResult>(command =>
            {
                Assert.Equal(
                    expected: DomainResupplyFocus.Medicine,
                    actual: command.Focus);
                Assert.Equal(
                    expected: DomainResupplyIntensity.High,
                    actual: command.Intensity);
                Assert.True(command.EmergencyOverride);
                Assert.Equal(
                    expected: districtId,
                    actual: command.FocusDistrictId);
                return CreateDispatchCityResupplyResult(
                    status: DispatchCityResupplyStatus.AuthorizationDenied,
                    cityId: cityId);
            });
            var controller = new StockpilesController(sender);

            IResult result = await controller.DispatchResupply(
                cityId: cityId,
                request: new DispatchCityResupplyRequest(
                    Focus: RequestResupplyFocus.Medicine,
                    Intensity: RequestResupplyIntensity.High,
                    DistrictId: districtId,
                    EmergencyOverride: true),
                cancellationToken: CancellationToken.None);

            DispatchCityResupplyView view = AssertResult<DispatchCityResupplyView>(
                result: result,
                expectedStatusCode: StatusCodes.Status409Conflict);
            Assert.Equal(
                expected: "AuthorizationDenied",
                actual: view.Status);
            Assert.Equal(
                expected: "Denied",
                actual: view.BudgetAuthorizationStatus);
            Assert.Equal(
                expected: "Medium",
                actual: view.AppliedIntensity);
        }

        [Fact]
        public async Task DispatchResupply_WhenEnumsAreOutOfRange_UsesFallbackMappings()
        {
            var cityId = Guid.Parse("4714ec2a-2745-4fc4-bdc2-a8df9428d77d");
            var sender = new FakeSender();
            sender.Handle<DispatchCityResupplyCommand, DispatchCityResupplyResult>(command =>
            {
                Assert.Equal(
                    expected: DomainResupplyFocus.All,
                    actual: command.Focus);
                Assert.Equal(
                    expected: DomainResupplyIntensity.Medium,
                    actual: command.Intensity);
                return CreateDispatchCityResupplyResult(
                    status: DispatchCityResupplyStatus.Scheduled,
                    cityId: cityId);
            });
            var controller = new StockpilesController(sender);

            IResult result = await controller.DispatchResupply(
                cityId: cityId,
                request: new DispatchCityResupplyRequest(
                    Focus: (RequestResupplyFocus)999,
                    Intensity: (RequestResupplyIntensity)999,
                    DistrictId: null,
                    EmergencyOverride: false),
                cancellationToken: CancellationToken.None);

            DispatchCityResupplyView view = AssertResult<DispatchCityResupplyView>(
                result: result,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Equal(
                expected: "Scheduled",
                actual: view.Status);
            Assert.Equal(
                expected: "Managed",
                actual: view.BudgetAuthorizationLevel);
            Assert.Equal(
                expected: 0.67m,
                actual: view.EmergencyWaterStockLevelIndex);
        }
    }
}
