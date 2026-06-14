using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure
{
    public sealed class GetCityOperationalBudgetPressureQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ForwardsCityIdToProjectionService()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var projectionService = new FakeCityOperationalBudgetPressureProjectionService();
            var handler = new GetCityOperationalBudgetPressureQueryHandler(projectionService);

            CityOperationalBudgetPressureDto result = await handler.Handle(
                request: new GetCityOperationalBudgetPressureQuery(cityId),
                cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId,
                actual: projectionService.RequestedCityId);
            Assert.Equal(
                expected: projectionService.Result,
                actual: result);
        }
    }
}
