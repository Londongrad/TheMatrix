using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Aggregates
{
    public sealed class CityHouseholdObligation
    {
        public Guid Id { get; private set; }
        public Guid CityId { get; private set; }
        public Guid HouseholdAccountId { get; private set; }
        public Guid ProviderBusinessId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public CityHouseholdObligationKind Kind { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public bool IsActive { get; private set; }
        public CityBudgetUnitKind UnitKind { get; private set; }
        public string UnitCode { get; private set; } = string.Empty;
        public string UnitDisplayName { get; private set; } = string.Empty;
        public string UnitSymbol { get; private set; } = string.Empty;
        public Money ChargeAmount { get; private set; } = null!;
        public Money TaxAmount { get; private set; } = null!;
        public DateTimeOffset? LastChargedAtUtc { get; private set; }
        public int ChargeCount { get; private set; }

        private CityHouseholdObligation()
        {
        }

        public CityHouseholdObligation(
            Guid id,
            Guid cityId,
            Guid householdAccountId,
            Guid providerBusinessId,
            string name,
            CityHouseholdObligationKind kind,
            DateTimeOffset createdAtUtc,
            CityBudgetUnitProfile unitProfile,
            Money chargeAmount,
            Money taxAmount)
        {
            Id = GuardHelper.AgainstEmptyGuid(id, nameof(id));
            CityId = GuardHelper.AgainstEmptyGuid(cityId, nameof(cityId));
            HouseholdAccountId = GuardHelper.AgainstEmptyGuid(householdAccountId, nameof(householdAccountId));
            ProviderBusinessId = GuardHelper.AgainstEmptyGuid(providerBusinessId, nameof(providerBusinessId));
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("Obligation name is required.", nameof(name))
                : name.Trim();
            Kind = kind;
            CreatedAtUtc = createdAtUtc;
            IsActive = true;
            ApplyUnitProfile(unitProfile);

            if (!chargeAmount.IsPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(chargeAmount), "Obligation charge amount must be positive.");
            }

            if (taxAmount.IsNegative || taxAmount.Amount > chargeAmount.Amount)
            {
                throw new ArgumentOutOfRangeException(nameof(taxAmount), "Tax amount must be between zero and total charge.");
            }

            ChargeAmount = chargeAmount;
            TaxAmount = taxAmount;
            ChargeCount = 0;
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
                    $"Obligation unit mismatch. Existing={UnitKind}:{UnitCode}, requested={requestedUnitProfile.Kind}:{requestedUnitProfile.Code}.");
            }
        }

        public void MarkCharged(DateTimeOffset chargedAtUtc)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Cannot charge an inactive obligation.");
            }

            LastChargedAtUtc = chargedAtUtc;
            ChargeCount++;
        }

        public void Deactivate()
        {
            IsActive = false;
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
