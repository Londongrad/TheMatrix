using Matrix.Economy.Application.UseCases.HouseholdAccounts.GetCityHouseholdAccounts;
using Matrix.Economy.Application.Tests.TestSupport;
using Xunit;
using static Matrix.Economy.Application.Tests.TestSupport.EconomyApplicationTestSupport;

namespace Matrix.Economy.Application.Tests.UseCases.HouseholdAccounts.GetCityHouseholdAccounts;

public sealed class GetCityHouseholdAccountsQueryHandlerTests
{
    [Fact]
    public async Task Handle_MapsHouseholdAccountsToDtos()
    {
        Guid cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var account = CreateHouseholdAccount(cityId, "Anderson Household", 300m);
        account.ReceivePayroll(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(120m));
        account.RecordConsumerPurchase(Matrix.BuildingBlocks.Domain.ValueObjects.Money.FromDecimal(45m));
        var repository = new FakeCityHouseholdAccountRepository
        {
            Accounts =
            [
                account,
                CreateHouseholdAccount(Guid.Parse("11111111-2222-3333-4444-555555555555"), "Other Household", 90m)
            ]
        };
        var handler = new GetCityHouseholdAccountsQueryHandler(repository);

        IReadOnlyList<Matrix.Economy.Application.UseCases.HouseholdAccounts.CityHouseholdAccountDto> result =
            await handler.Handle(new GetCityHouseholdAccountsQuery(cityId), CancellationToken.None);

        Assert.Equal(cityId, repository.RequestedCityId);
        Assert.Single(result);
        Assert.Equal("Anderson Household", result[0].Name);
        Assert.Equal(375m, result[0].Balance);
        Assert.Equal(120m, result[0].TotalPayrollIncome);
        Assert.Equal(45m, result[0].TotalConsumerSpending);
    }
}
