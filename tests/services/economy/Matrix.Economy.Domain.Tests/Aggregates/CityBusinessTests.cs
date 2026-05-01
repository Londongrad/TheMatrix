using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Aggregates;

public sealed class CityBusinessTests
{
    [Fact]
    public void Constructor_WhenArgumentsAreValid_NormalizesFieldsAndInitializesTotals()
    {
        Guid cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var business = CreateBusiness(
            cityId: cityId,
            kind: CityBusinessKind.Service,
            name: " Central Services ",
            externalReferenceCode: " ext-1 ",
            templateKey: " service-template ");

        Assert.Equal(cityId, business.CityId);
        Assert.Equal("Central Services", business.Name);
        Assert.Equal("ext-1", business.ExternalReferenceCode);
        Assert.Equal("service-template", business.TemplateKey);
        Assert.Equal(CityBudgetUnitKind.Currency, business.UnitKind);
        Assert.Equal("CR", business.UnitCode);
        Assert.Equal("Credits", business.UnitDisplayName);
        Assert.Equal("₡", business.UnitSymbol);
        Assert.Equal(Money.FromDecimal(1000m), business.Balance);
        Assert.Equal(Money.FromDecimal(1000m), business.TotalCapitalInjections);
        Assert.Equal(Money.Zero, business.TaxReserve);
        Assert.Equal(Money.Zero, business.TotalRetailTurnover);
    }

    [Fact]
    public void EnsureCanIssuePayroll_WhenKindDoesNotSupportPayroll_ThrowsInvalidOperationException()
    {
        CityBusiness business = CreateBusiness(
            cityId: Guid.NewGuid(),
            kind: CityBusinessKind.RetailStore);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => business.EnsureCanIssuePayroll());

        Assert.Equal("Business kind 'RetailStore' cannot issue payroll.", exception.Message);
    }

    [Fact]
    public void RecordRetailSale_WhenArgumentsAreValid_UpdatesBalanceReserveAndTotals()
    {
        CityBusiness business = CreateBusiness(
            cityId: Guid.NewGuid(),
            kind: CityBusinessKind.RetailStore);

        business.RecordRetailSale(
            grossAmount: Money.FromDecimal(120m),
            salesTaxAmount: Money.FromDecimal(20m));

        Assert.Equal(Money.FromDecimal(1120m), business.Balance);
        Assert.Equal(Money.FromDecimal(20m), business.TaxReserve);
        Assert.Equal(Money.FromDecimal(120m), business.TotalRetailTurnover);
        Assert.Equal(Money.FromDecimal(100m), business.TotalNetSalesRevenue);
    }

    [Fact]
    public void SettlePayroll_WhenBalanceIsInsufficient_ReturnsPartialOutcomeAndSubtractsPaidAmount()
    {
        CityBusiness business = CreateBusiness(
            cityId: Guid.NewGuid(),
            kind: CityBusinessKind.Service,
            initialCapital: 150m);

        CityBusinessPayrollSettlementOutcome outcome = business.SettlePayroll(
            requestedGrossPayroll: Money.FromDecimal(200m),
            requestedIncomeTax: Money.FromDecimal(40m));

        Assert.True(outcome.IsPartiallyPaid);
        Assert.False(outcome.IsFullyPaid);
        Assert.False(outcome.IsMissed);
        Assert.Equal(Money.FromDecimal(150m), outcome.PaidGrossPayroll);
        Assert.Equal(Money.FromDecimal(30m), outcome.PaidIncomeTax);
        Assert.Equal(Money.FromDecimal(120m), outcome.PaidNetPayroll);
        Assert.Equal(Money.FromDecimal(50m), outcome.GrossShortfall);
        Assert.Equal(0.75m, outcome.FulfillmentRatio);
        Assert.Equal(Money.Zero, business.Balance);
        Assert.Equal(Money.FromDecimal(150m), business.TotalOperatingExpenses);
    }

    [Fact]
    public void RemitTax_WhenAmountExceedsReserve_ThrowsInvalidOperationException()
    {
        CityBusiness business = CreateBusiness(
            cityId: Guid.NewGuid(),
            kind: CityBusinessKind.RetailStore);
        business.RecordRetailSale(
            grossAmount: Money.FromDecimal(120m),
            salesTaxAmount: Money.FromDecimal(20m));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => business.RemitTax(Money.FromDecimal(25m)));

        Assert.Equal("Cannot remit more tax than the current reserve.", exception.Message);
    }

    [Fact]
    public void RemitTax_WhenAmountIsWithinReserve_UpdatesBalanceReserveAndTotals()
    {
        CityBusiness business = CreateBusiness(
            cityId: Guid.NewGuid(),
            kind: CityBusinessKind.RetailStore);
        business.RecordRetailSale(
            grossAmount: Money.FromDecimal(120m),
            salesTaxAmount: Money.FromDecimal(20m));

        business.RemitTax(Money.FromDecimal(15m));

        Assert.Equal(Money.FromDecimal(1105m), business.Balance);
        Assert.Equal(Money.FromDecimal(5m), business.TaxReserve);
        Assert.Equal(Money.FromDecimal(15m), business.TotalTaxRemitted);
    }

    private static CityBusiness CreateBusiness(
        Guid cityId,
        CityBusinessKind kind,
        string name = "Business",
        string? externalReferenceCode = "external-1",
        string? templateKey = "template-1",
        decimal initialCapital = 1000m)
    {
        return new CityBusiness(
            id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            cityId: cityId,
            name: name,
            externalReferenceCode: externalReferenceCode,
            templateKey: templateKey,
            kind: kind,
            createdAtUtc: new DateTimeOffset(2048, 2, 3, 4, 5, 6, TimeSpan.Zero),
            unitProfile: new CityBudgetUnitProfile(
                Kind: CityBudgetUnitKind.Currency,
                Code: "CR",
                DisplayName: "Credits",
                Symbol: "₡"),
            initialCapital: Money.FromDecimal(initialCapital));
    }
}
