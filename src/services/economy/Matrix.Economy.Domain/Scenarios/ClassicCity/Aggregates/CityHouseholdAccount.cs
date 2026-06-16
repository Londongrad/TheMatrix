using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates
{
    public sealed class CityHouseholdAccount
    {
        private CityHouseholdAccount() { }

        public CityHouseholdAccount(
            Guid id,
            Guid cityId,
            string name,
            string? externalReferenceCode,
            DateTimeOffset createdAtUtc,
            CityBudgetUnitProfile unitProfile,
            Money openingBalance)
        {
            Id = GuardHelper.AgainstEmptyGuid(
                id: id,
                propertyName: nameof(id));
            CityId = GuardHelper.AgainstEmptyGuid(
                id: cityId,
                propertyName: nameof(cityId));
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException(
                    message: "Household account name is required.",
                    paramName: nameof(name))
                : name.Trim();
            ExternalReferenceCode = string.IsNullOrWhiteSpace(externalReferenceCode)
                ? null
                : externalReferenceCode.Trim();
            CreatedAtUtc = createdAtUtc;
            ApplyUnitProfile(unitProfile);

            if (openingBalance.IsNegative)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(openingBalance),
                    message: "Opening balance cannot be negative.");

            Balance = openingBalance;
            TotalOpeningBalance = openingBalance;
            TotalPayrollIncome = Money.Zero;
            TotalConsumerSpending = Money.Zero;
        }

        public Guid Id { get; private set; }
        public Guid CityId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string? ExternalReferenceCode { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public CityBudgetUnitKind UnitKind { get; private set; }
        public string UnitCode { get; private set; } = string.Empty;
        public string UnitDisplayName { get; private set; } = string.Empty;
        public string UnitSymbol { get; private set; } = string.Empty;
        public Money Balance { get; private set; } = null!;
        public Money TotalOpeningBalance { get; private set; } = null!;
        public Money TotalPayrollIncome { get; private set; } = null!;
        public Money TotalConsumerSpending { get; private set; } = null!;

        public CityBudgetUnitProfile GetUnitProfile()
        {
            return new CityBudgetUnitProfile(
                Kind: UnitKind,
                Code: UnitCode,
                DisplayName: UnitDisplayName,
                Symbol: UnitSymbol);
        }

        public void EnsureCompatibleUnit(CityBudgetUnitProfile requestedUnitProfile)
        {
            if (!string.Equals(
                    a: UnitCode,
                    b: requestedUnitProfile.Code,
                    comparisonType: StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    a: UnitDisplayName,
                    b: requestedUnitProfile.DisplayName,
                    comparisonType: StringComparison.Ordinal) ||
                !string.Equals(
                    a: UnitSymbol,
                    b: requestedUnitProfile.Symbol,
                    comparisonType: StringComparison.Ordinal) ||
                UnitKind != requestedUnitProfile.Kind)
                throw new InvalidOperationException(
                    $"Household account unit mismatch. Existing={UnitKind}:{UnitCode}, requested={requestedUnitProfile.Kind}:{requestedUnitProfile.Code}.");
        }

        public void ReceivePayroll(Money amount)
        {
            if (!amount.IsPositive)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(amount),
                    message: "Payroll income must be positive.");

            Balance = Balance.Add(amount);
            TotalPayrollIncome = TotalPayrollIncome.Add(amount);
        }

        public void RecordConsumerPurchase(Money amount)
        {
            if (!amount.IsPositive)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(amount),
                    message: "Consumer purchase amount must be positive.");

            if (amount.Amount > Balance.Amount)
                throw new InvalidOperationException(
                    "Household account does not have enough balance for this purchase.");

            Balance = Balance.Subtract(amount);
            TotalConsumerSpending = TotalConsumerSpending.Add(amount);
        }

        public void RecordObligationCharge(Money amount)
        {
            RecordConsumerPurchase(amount);
        }

        private void ApplyUnitProfile(CityBudgetUnitProfile unitProfile)
        {
            UnitKind = unitProfile.Kind;
            UnitCode = string.IsNullOrWhiteSpace(unitProfile.Code)
                ? throw new ArgumentException(
                    message: "Unit code is required.",
                    paramName: nameof(unitProfile))
                : unitProfile.Code.Trim()
                   .ToUpperInvariant();
            UnitDisplayName = string.IsNullOrWhiteSpace(unitProfile.DisplayName)
                ? throw new ArgumentException(
                    message: "Unit display name is required.",
                    paramName: nameof(unitProfile))
                : unitProfile.DisplayName.Trim();
            UnitSymbol = string.IsNullOrWhiteSpace(unitProfile.Symbol)
                ? throw new ArgumentException(
                    message: "Unit symbol is required.",
                    paramName: nameof(unitProfile))
                : unitProfile.Symbol.Trim();
        }
    }
}
