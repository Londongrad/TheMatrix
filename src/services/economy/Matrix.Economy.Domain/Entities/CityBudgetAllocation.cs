using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Entities
{
    public sealed class CityBudgetAllocation
    {
        public Guid Id { get; private set; }
        public Guid CityId { get; private set; }
        public CityBudgetCategory Category { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }
        public CityBudgetUnitKind UnitKind { get; private set; }
        public string UnitCode { get; private set; } = string.Empty;
        public string UnitDisplayName { get; private set; } = string.Empty;
        public string UnitSymbol { get; private set; } = string.Empty;
        public Money TargetAmount { get; private set; } = null!;
        public Money TotalSpent { get; private set; } = null!;

        private CityBudgetAllocation()
        {
        }

        public CityBudgetAllocation(
            Guid id,
            Guid cityId,
            CityBudgetCategory category,
            DateTimeOffset createdAtUtc,
            CityBudgetUnitProfile unitProfile,
            Money targetAmount)
        {
            Id = GuardHelper.AgainstEmptyGuid(id, nameof(id));
            CityId = GuardHelper.AgainstEmptyGuid(cityId, nameof(cityId));
            Category = category;
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = createdAtUtc;
            ApplyUnitProfile(unitProfile);

            if (targetAmount.IsNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(targetAmount), "Target amount cannot be negative.");
            }

            TargetAmount = targetAmount;
            TotalSpent = Money.Zero;
        }

        public Money GetAvailableAmount()
        {
            return TargetAmount.Subtract(TotalSpent);
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
                    $"Budget allocation unit mismatch. Existing={UnitKind}:{UnitCode}, requested={requestedUnitProfile.Kind}:{requestedUnitProfile.Code}.");
            }
        }

        public void SetTargetAmount(Money targetAmount, DateTimeOffset updatedAtUtc)
        {
            if (targetAmount.IsNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(targetAmount), "Target amount cannot be negative.");
            }

            TargetAmount = targetAmount;
            UpdatedAtUtc = updatedAtUtc;
        }

        public void RecordExpense(Money amount, DateTimeOffset updatedAtUtc)
        {
            if (!amount.IsPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Expense amount must be positive.");
            }

            TotalSpent = TotalSpent.Add(amount);
            UpdatedAtUtc = updatedAtUtc;
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
