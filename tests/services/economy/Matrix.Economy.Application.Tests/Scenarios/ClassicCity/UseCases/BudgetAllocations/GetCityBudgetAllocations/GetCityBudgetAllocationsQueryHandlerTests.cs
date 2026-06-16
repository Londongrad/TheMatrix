using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.BudgetAllocations.GetCityBudgetAllocations;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.BudgetAllocations.GetCityBudgetAllocations
{
    public sealed class GetCityBudgetAllocationsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_MapsCityAllocationsToDtos()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var allocationRepository = new FakeCityBudgetAllocationRepository
            {
                Allocations =
                [
                    CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Infrastructure,
                        targetAmount: 500m,
                        spentAmount: 140m),
                    CreateAllocation(
                        cityId: cityId,
                        category: CityBudgetCategory.Healthcare,
                        targetAmount: 300m,
                        spentAmount: 20m),
                    CreateAllocation(
                        cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                        category: CityBudgetCategory.General,
                        targetAmount: 200m,
                        spentAmount: 15m)
                ]
            };
            var handler = new GetCityBudgetAllocationsQueryHandler(allocationRepository);

            IReadOnlyList<CityBudgetAllocationDto> result =
                await handler.Handle(
                    request: new GetCityBudgetAllocationsQuery(cityId),
                    cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId,
                actual: allocationRepository.RequestedCityId);
            Assert.Equal(
                expected: 2,
                actual: result.Count);
            Assert.Equal(
                expected: "Infrastructure",
                actual: result[0].Category);
            Assert.Equal(
                expected: 500m,
                actual: result[0].TargetAmount);
            Assert.Equal(
                expected: 140m,
                actual: result[0].TotalSpent);
            Assert.Equal(
                expected: 360m,
                actual: result[0].AvailableAmount);
            Assert.Equal(
                expected: "Healthcare",
                actual: result[1].Category);
            Assert.Equal(
                expected: 280m,
                actual: result[1].AvailableAmount);
        }
    }
}
