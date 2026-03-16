using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Aggregates
{
    public sealed class CityHouseholdObligation
    {
        private CityHouseholdObligation() { }

        public CityHouseholdObligation(
            Guid id,
            Guid cityId,
            Guid householdAccountId,
            Guid providerBusinessId,
            string name,
            CityHouseholdObligationKind kind,
            CityHouseholdObligationBillingCadence billingCadence,
            DateTimeOffset createdAtUtc,
            DateTimeOffset firstChargeDueAtUtc,
            CityBudgetUnitProfile unitProfile,
            Money chargeAmount,
            Money taxAmount)
        {
            Id = GuardHelper.AgainstEmptyGuid(
                id: id,
                propertyName: nameof(id));
            CityId = GuardHelper.AgainstEmptyGuid(
                id: cityId,
                propertyName: nameof(cityId));
            HouseholdAccountId = GuardHelper.AgainstEmptyGuid(
                id: householdAccountId,
                propertyName: nameof(householdAccountId));
            ProviderBusinessId = GuardHelper.AgainstEmptyGuid(
                id: providerBusinessId,
                propertyName: nameof(providerBusinessId));
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException(
                    message: "Obligation name is required.",
                    paramName: nameof(name))
                : name.Trim();
            Kind = kind;
            BillingCadence = billingCadence;
            CreatedAtUtc = createdAtUtc;
            IsActive = true;
            ApplyUnitProfile(unitProfile);

            if (!chargeAmount.IsPositive)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(chargeAmount),
                    message: "Obligation charge amount must be positive.");

            if (taxAmount.IsNegative || taxAmount.Amount > chargeAmount.Amount)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(taxAmount),
                    message: "Tax amount must be between zero and total charge.");

            ChargeAmount = chargeAmount;
            TaxAmount = taxAmount;
            NextChargeDueAtUtc = firstChargeDueAtUtc;
            ChargeCount = 0;
        }

        public Guid Id { get; private set; }
        public Guid CityId { get; private set; }
        public Guid HouseholdAccountId { get; private set; }
        public Guid ProviderBusinessId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public CityHouseholdObligationKind Kind { get; private set; }
        public CityHouseholdObligationBillingCadence BillingCadence { get; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public bool IsActive { get; private set; }
        public CityBudgetUnitKind UnitKind { get; private set; }
        public string UnitCode { get; private set; } = string.Empty;
        public string UnitDisplayName { get; private set; } = string.Empty;
        public string UnitSymbol { get; private set; } = string.Empty;
        public Money ChargeAmount { get; private set; } = null!;
        public Money TaxAmount { get; private set; } = null!;
        public DateTimeOffset NextChargeDueAtUtc { get; private set; }
        public DateTimeOffset? LastChargedAtUtc { get; private set; }
        public int ChargeCount { get; private set; }

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
                    $"Obligation unit mismatch. Existing={UnitKind}:{UnitCode}, requested={requestedUnitProfile.Kind}:{requestedUnitProfile.Code}.");
        }

        public void MarkCharged(DateTimeOffset chargedAtUtc)
        {
            if (!IsActive)
                throw new InvalidOperationException("Cannot charge an inactive obligation.");

            LastChargedAtUtc = chargedAtUtc;
            ChargeCount++;
            NextChargeDueAtUtc = BillingCadence switch
            {
                CityHouseholdObligationBillingCadence.Daily => chargedAtUtc.AddDays(1),
                CityHouseholdObligationBillingCadence.Weekly => chargedAtUtc.AddDays(7),
                _ => chargedAtUtc.AddMonths(1)
            };
        }

        public bool IsDue(DateTimeOffset asOfUtc)
        {
            return IsActive && NextChargeDueAtUtc <= asOfUtc;
        }

        public void Deactivate()
        {
            IsActive = false;
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
