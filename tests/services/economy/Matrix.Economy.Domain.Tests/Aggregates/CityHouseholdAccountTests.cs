using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Aggregates;

public sealed class CityHouseholdAccountTests
{
    [Fact]
    public void Constructor_WhenArgumentsAreValid_NormalizesFieldsAndInitializesTotals()
    {
        Guid cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var account = CreateAccount(
            cityId: cityId,
            name: " Household A ",
            externalReferenceCode: " ext-1 ",
            openingBalance: 250m);

        Assert.Equal(cityId, account.CityId);
        Assert.Equal("Household A", account.Name);
        Assert.Equal("ext-1", account.ExternalReferenceCode);
        Assert.Equal(CityBudgetUnitKind.Currency, account.UnitKind);
        Assert.Equal("CR", account.UnitCode);
        Assert.Equal("Credits", account.UnitDisplayName);
        Assert.Equal("₡", account.UnitSymbol);
        Assert.Equal(Money.FromDecimal(250m), account.Balance);
        Assert.Equal(Money.FromDecimal(250m), account.TotalOpeningBalance);
        Assert.Equal(Money.Zero, account.TotalPayrollIncome);
        Assert.Equal(Money.Zero, account.TotalConsumerSpending);
    }

    [Fact]
    public void ReceivePayroll_WhenAmountIsPositive_UpdatesBalanceAndIncome()
    {
        CityHouseholdAccount account = CreateAccount(Guid.NewGuid(), openingBalance: 100m);

        account.ReceivePayroll(Money.FromDecimal(75m));

        Assert.Equal(Money.FromDecimal(175m), account.Balance);
        Assert.Equal(Money.FromDecimal(75m), account.TotalPayrollIncome);
    }

    [Fact]
    public void RecordConsumerPurchase_WhenBalanceIsEnough_UpdatesBalanceAndSpending()
    {
        CityHouseholdAccount account = CreateAccount(Guid.NewGuid(), openingBalance: 200m);

        account.RecordConsumerPurchase(Money.FromDecimal(60m));

        Assert.Equal(Money.FromDecimal(140m), account.Balance);
        Assert.Equal(Money.FromDecimal(60m), account.TotalConsumerSpending);
    }

    [Fact]
    public void RecordConsumerPurchase_WhenBalanceIsInsufficient_ThrowsInvalidOperationException()
    {
        CityHouseholdAccount account = CreateAccount(Guid.NewGuid(), openingBalance: 50m);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => account.RecordConsumerPurchase(Money.FromDecimal(60m)));

        Assert.Equal("Household account does not have enough balance for this purchase.", exception.Message);
    }

    [Fact]
    public void EnsureCompatibleUnit_WhenProfileDiffers_ThrowsInvalidOperationException()
    {
        CityHouseholdAccount account = CreateAccount(Guid.NewGuid(), openingBalance: 100m);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => account.EnsureCompatibleUnit(
                new CityBudgetUnitProfile(
                    Kind: CityBudgetUnitKind.Resource,
                    Code: "res",
                    DisplayName: "Resources",
                    Symbol: "R")));

        Assert.Contains("Household account unit mismatch.", exception.Message);
    }

    private static CityHouseholdAccount CreateAccount(
        Guid cityId,
        string name = "Household",
        string? externalReferenceCode = "external-1",
        decimal openingBalance = 100m)
    {
        return new CityHouseholdAccount(
            id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            cityId: cityId,
            name: name,
            externalReferenceCode: externalReferenceCode,
            createdAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero),
            unitProfile: new CityBudgetUnitProfile(
                Kind: CityBudgetUnitKind.Currency,
                Code: "CR",
                DisplayName: "Credits",
                Symbol: "₡"),
            openingBalance: Money.FromDecimal(openingBalance));
    }
}
