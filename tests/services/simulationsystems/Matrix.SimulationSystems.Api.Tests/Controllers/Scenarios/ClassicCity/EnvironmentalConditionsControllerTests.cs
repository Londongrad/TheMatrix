using Matrix.SimulationSystems.Api.Controllers.Scenarios.ClassicCity;
using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    GetCityEnvironmentalConditions;
using Matrix.SimulationSystems.Contracts.Scenarios.ClassicCity.EnvironmentalConditions.Views;
using Microsoft.AspNetCore.Http;
using Xunit;
using static Matrix.SimulationSystems.Api.Tests.TestSupport.SimulationSystemsApiTestSupport;

namespace Matrix.SimulationSystems.Api.Tests.Controllers.Scenarios.ClassicCity
{
    public sealed class EnvironmentalConditionsControllerTests
    {
        [Fact]
        public async Task Get_WhenConditionsAreMissing_ReturnsNotFound()
        {
            var cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
            var sender = new FakeSender();
            sender.Handle<GetCityEnvironmentalConditionsQuery, CityEnvironmentalConditionsDto?>(_ => null);
            var controller = new EnvironmentalConditionsController(sender);

            IResult result = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            AssertStatus(
                result: result,
                expectedStatusCode: StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task Get_WhenConditionsExist_ReturnsMappedView()
        {
            var cityId = Guid.Parse("6f3a95ea-c9c2-43b2-99a8-ea46d27a7a14");
            var sender = new FakeSender();
            sender.Handle<GetCityEnvironmentalConditionsQuery, CityEnvironmentalConditionsDto?>(query =>
            {
                Assert.Equal(
                    expected: cityId,
                    actual: query.CityId);
                return CreateEnvironmentalConditionsDto(cityId);
            });
            var controller = new EnvironmentalConditionsController(sender);

            IResult result = await controller.Get(
                cityId: cityId,
                cancellationToken: CancellationToken.None);

            CityEnvironmentalConditionsView view = AssertResult<CityEnvironmentalConditionsView>(
                result: result,
                expectedStatusCode: StatusCodes.Status200OK);
            Assert.Equal(
                expected: cityId,
                actual: view.CityId);
            Assert.Equal(
                expected: "SystemsDegradation",
                actual: view.EffectivePhase);
            Assert.Equal(
                expected: 0.72m,
                actual: view.ResourceSupply.Fuel.StockLevelIndex);
            Assert.Equal(
                expected: "Drainage",
                actual: view.Drainage.Kind);
            Assert.Equal(
                expected: 0.14m,
                actual: view.Sanitation.BacklogIndex);
        }
    }
}
