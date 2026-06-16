using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Xunit;

namespace Matrix.Economy.Domain.Tests.Aggregates
{
    public sealed class CityHouseholdAccountTests
    {
        [Fact]
        public void Constructor_WhenArgumentsAreValid_NormalizesFieldsAndInitializesTotals()
        {
            var cityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            CityHouseholdAccount account = CreateAccount(
                cityId: cityId,
                name: " Household A ",
                externalReferenceCode: " ext-1 ",
                openingBalance: 250m);

            Assert.Equal(
                expected: cityId,
                actual: account.CityId);
            Assert.Equal(
                expected: "Household A",
                actual: account.Name);
            Assert.Equal(
                expected: "ext-1",
                actual: account.ExternalReferenceCode);
            Assert.Equal(
                expected: CityBudgetUnitKind.Currency,
                actual: account.UnitKind);
            Assert.Equal(
                expected: "CR",
                actual: account.UnitCode);
            Assert.Equal(
                expected: "Credits",
                actual: account.UnitDisplayName);
            Assert.Equal(
                expected: "₡",
                actual: account.UnitSymbol);
            Assert.Equal(
                expected: Money.FromDecimal(250m),
                actual: account.Balance);
            Assert.Equal(
                expected: Money.FromDecimal(250m),
                actual: account.TotalOpeningBalance);
            Assert.Equal(
                expected: Money.Zero,
                actual: account.TotalPayrollIncome);
            Assert.Equal(
                expected: Money.Zero,
                actual: account.TotalConsumerSpending);
        }

        [Fact]
        public void ReceivePayroll_WhenAmountIsPositive_UpdatesBalanceAndIncome()
        {
            CityHouseholdAccount account = CreateAccount(
                cityId: Guid.NewGuid(),
                openingBalance: 100m);

            account.ReceivePayroll(Money.FromDecimal(75m));

            Assert.Equal(
                expected: Money.FromDecimal(175m),
                actual: account.Balance);
            Assert.Equal(
                expected: Money.FromDecimal(75m),
                actual: account.TotalPayrollIncome);
        }

        [Fact]
        public void RecordConsumerPurchase_WhenBalanceIsEnough_UpdatesBalanceAndSpending()
        {
            CityHouseholdAccount account = CreateAccount(
                cityId: Guid.NewGuid(),
                openingBalance: 200m);

            account.RecordConsumerPurchase(Money.FromDecimal(60m));

            Assert.Equal(
                expected: Money.FromDecimal(140m),
                actual: account.Balance);
            Assert.Equal(
                expected: Money.FromDecimal(60m),
                actual: account.TotalConsumerSpending);
        }

        [Fact]
        public void RecordConsumerPurchase_WhenBalanceIsInsufficient_ThrowsInvalidOperationException()
        {
            CityHouseholdAccount account = CreateAccount(
                cityId: Guid.NewGuid(),
                openingBalance: 50m);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => account.RecordConsumerPurchase(Money.FromDecimal(60m)));

            Assert.Equal(
                expected: "Household account does not have enough balance for this purchase.",
                actual: exception.Message);
        }

        [Fact]
        public void EnsureCompatibleUnit_WhenProfileDiffers_ThrowsInvalidOperationException()
        {
            CityHouseholdAccount account = CreateAccount(
                cityId: Guid.NewGuid(),
                openingBalance: 100m);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(()
                => account.EnsureCompatibleUnit(
                    new CityBudgetUnitProfile(
                        Kind: CityBudgetUnitKind.Resource,
                        Code: "res",
                        DisplayName: "Resources",
                        Symbol: "R")));

            Assert.Contains(
                expectedSubstring: "Household account unit mismatch.",
                actualString: exception.Message);
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
                createdAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 2,
                    day: 3,
                    hour: 4,
                    minute: 5,
                    second: 6,
                    offset: TimeSpan.Zero),
                unitProfile: new CityBudgetUnitProfile(
                    Kind: CityBudgetUnitKind.Currency,
                    Code: "CR",
                    DisplayName: "Credits",
                    Symbol: "₡"),
                openingBalance: Money.FromDecimal(openingBalance));
        }
    }
}
