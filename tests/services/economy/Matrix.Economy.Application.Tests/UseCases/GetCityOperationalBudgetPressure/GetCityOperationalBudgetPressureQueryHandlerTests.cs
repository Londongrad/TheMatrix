using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Application.Tests.TestSupport;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.GetCityOperationalBudgetPressure;

public sealed class GetCityOperationalBudgetPressureQueryHandlerTests
{
    [Fact]
    public async Task Handle_ForwardsCityIdToProjectionService()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var projectionService = new FakeCityOperationalBudgetPressureProjectionService();
        var handler = new GetCityOperationalBudgetPressureQueryHandler(projectionService);

        CityOperationalBudgetPressureDto result = await handler.Handle(
            new GetCityOperationalBudgetPressureQuery(cityId),
            CancellationToken.None);

        Assert.Equal(cityId, projectionService.RequestedCityId);
        Assert.Equal(projectionService.Result, result);
    }
}
