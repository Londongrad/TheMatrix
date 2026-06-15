using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccounts;
using Matrix.Economy.Domain.Aggregates;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.Scenarios.ClassicCity.UseCases.HouseholdAccounts.GetCityHouseholdAccounts
{
    public sealed class GetCityHouseholdAccountsQueryHandlerTests
    {
        [Fact]
        public async Task Handle_MapsHouseholdAccountsToDtos()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            CityHouseholdAccount account = CreateHouseholdAccount(
                cityId: cityId,
                name: "Anderson Household",
                openingBalance: 300m);
            account.ReceivePayroll(Money.FromDecimal(120m));
            account.RecordConsumerPurchase(Money.FromDecimal(45m));
            var repository = new FakeCityHouseholdAccountRepository
            {
                Accounts =
                [
                    account,
                    CreateHouseholdAccount(
                        cityId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
                        name: "Other Household",
                        openingBalance: 90m)
                ]
            };
            var handler = new GetCityHouseholdAccountsQueryHandler(repository);

            IReadOnlyList<CityHouseholdAccountDto> result =
                await handler.Handle(
                    request: new GetCityHouseholdAccountsQuery(cityId),
                    cancellationToken: CancellationToken.None);

            Assert.Equal(
                expected: cityId,
                actual: repository.RequestedCityId);
            Assert.Single(result);
            Assert.Equal(
                expected: "Anderson Household",
                actual: result[0].Name);
            Assert.Equal(
                expected: 375m,
                actual: result[0].Balance);
            Assert.Equal(
                expected: 120m,
                actual: result[0].TotalPayrollIncome);
            Assert.Equal(
                expected: 45m,
                actual: result[0].TotalConsumerSpending);
        }
    }
}
