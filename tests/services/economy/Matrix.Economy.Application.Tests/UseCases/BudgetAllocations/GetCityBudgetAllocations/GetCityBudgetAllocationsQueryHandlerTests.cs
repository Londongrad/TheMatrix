using Matrix.Economy.Application.UseCases.BudgetAllocations.GetCityBudgetAllocations;
using Matrix.Economy.Application.Tests.TestSupport;
using Matrix.Economy.Domain.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.BudgetAllocations.GetCityBudgetAllocations;

public sealed class GetCityBudgetAllocationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsCityAllocationsToDtos()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var allocationRepository = new FakeCityBudgetAllocationRepository
        {
            Allocations =
            [
                CreateAllocation(cityId, CityBudgetCategory.Infrastructure, 500m, 140m),
                CreateAllocation(cityId, CityBudgetCategory.Healthcare, 300m, 20m),
                CreateAllocation(Guid.Parse("11111111-2222-3333-4444-555555555555"), CityBudgetCategory.General, 200m, 15m)
            ]
        };
        var handler = new GetCityBudgetAllocationsQueryHandler(allocationRepository);

        IReadOnlyList<Matrix.Economy.Application.UseCases.BudgetAllocations.CityBudgetAllocationDto> result =
            await handler.Handle(new GetCityBudgetAllocationsQuery(cityId), CancellationToken.None);

        Assert.Equal(cityId, allocationRepository.RequestedCityId);
        Assert.Equal(2, result.Count);
        Assert.Equal("Infrastructure", result[0].Category);
        Assert.Equal(500m, result[0].TargetAmount);
        Assert.Equal(140m, result[0].TotalSpent);
        Assert.Equal(360m, result[0].AvailableAmount);
        Assert.Equal("Healthcare", result[1].Category);
        Assert.Equal(280m, result[1].AvailableAmount);
    }
}
