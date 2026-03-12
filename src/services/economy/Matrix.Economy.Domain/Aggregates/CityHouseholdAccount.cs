using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Aggregates
{
    public sealed class CityHouseholdAccount
    {
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

        private CityHouseholdAccount()
        {
        }

        public CityHouseholdAccount(
            Guid id,
            Guid cityId,
            string name,
            string? externalReferenceCode,
            DateTimeOffset createdAtUtc,
            CityBudgetUnitProfile unitProfile,
            Money openingBalance)
        {
            Id = GuardHelper.AgainstEmptyGuid(id, nameof(id));
            CityId = GuardHelper.AgainstEmptyGuid(cityId, nameof(cityId));
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("Household account name is required.", nameof(name))
                : name.Trim();
            ExternalReferenceCode = string.IsNullOrWhiteSpace(externalReferenceCode)
                ? null
                : externalReferenceCode.Trim();
            CreatedAtUtc = createdAtUtc;
            ApplyUnitProfile(unitProfile);

            if (openingBalance.IsNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(openingBalance), "Opening balance cannot be negative.");
            }

            Balance = openingBalance;
            TotalOpeningBalance = openingBalance;
            TotalPayrollIncome = Money.Zero;
            TotalConsumerSpending = Money.Zero;
        }

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
            if (!string.Equals(UnitCode, requestedUnitProfile.Code, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(UnitDisplayName, requestedUnitProfile.DisplayName, StringComparison.Ordinal)
                || !string.Equals(UnitSymbol, requestedUnitProfile.Symbol, StringComparison.Ordinal)
                || UnitKind != requestedUnitProfile.Kind)
            {
                throw new InvalidOperationException(
                    $"Household account unit mismatch. Existing={UnitKind}:{UnitCode}, requested={requestedUnitProfile.Kind}:{requestedUnitProfile.Code}.");
            }
        }

        public void ReceivePayroll(Money amount)
        {
            if (!amount.IsPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Payroll income must be positive.");
            }

            Balance = Balance.Add(amount);
            TotalPayrollIncome = TotalPayrollIncome.Add(amount);
        }

        public void RecordConsumerPurchase(Money amount)
        {
            if (!amount.IsPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Consumer purchase amount must be positive.");
            }

            if (amount.Amount > Balance.Amount)
            {
                throw new InvalidOperationException("Household account does not have enough balance for this purchase.");
            }

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
                ? throw new ArgumentException("Unit code is required.", nameof(unitProfile))
                : unitProfile.Code.Trim().ToUpperInvariant();
            UnitDisplayName = string.IsNullOrWhiteSpace(unitProfile.DisplayName)
                ? throw new ArgumentException("Unit display name is required.", nameof(unitProfile))
                : unitProfile.DisplayName.Trim();
            UnitSymbol = string.IsNullOrWhiteSpace(unitProfile.Symbol)
                ? throw new ArgumentException("Unit symbol is required.", nameof(unitProfile))
                : unitProfile.Symbol.Trim();
        }
    }
}
