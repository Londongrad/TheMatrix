using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates
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
            BaseChargeAmount = chargeAmount;
            BaseTaxAmount = taxAmount;
            NextChargeDueAtUtc = firstChargeDueAtUtc;
            ChargeCount = 0;
            MissedChargeCount = 0;
        }

        public Guid Id { get; private set; }
        public Guid CityId { get; private set; }
        public Guid HouseholdAccountId { get; private set; }
        public Guid ProviderBusinessId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public CityHouseholdObligationKind Kind { get; }
        public CityHouseholdObligationBillingCadence BillingCadence { get; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public bool IsActive { get; private set; }
        public CityBudgetUnitKind UnitKind { get; private set; }
        public string UnitCode { get; private set; } = string.Empty;
        public string UnitDisplayName { get; private set; } = string.Empty;
        public string UnitSymbol { get; private set; } = string.Empty;
        public Money BaseChargeAmount { get; } = null!;
        public Money BaseTaxAmount { get; } = null!;
        public Money ChargeAmount { get; private set; } = null!;
        public Money TaxAmount { get; private set; } = null!;
        public DateTimeOffset NextChargeDueAtUtc { get; private set; }
        public DateTimeOffset? LastChargedAtUtc { get; private set; }
        public DateTimeOffset? LastChargeAttemptedAtUtc { get; private set; }
        public DateTimeOffset? FirstMissedChargeDueAtUtc { get; private set; }
        public DateTimeOffset? ServiceCutoffAtUtc { get; private set; }
        public DateTimeOffset? EvictionNoticeIssuedAtUtc { get; private set; }
        public DateTimeOffset? EvictionEligibleAtUtc { get; private set; }
        public int ChargeCount { get; private set; }
        public int MissedChargeCount { get; private set; }
        public bool HasActiveDelinquency => FirstMissedChargeDueAtUtc.HasValue;
        public bool HasServiceCutoff => ServiceCutoffAtUtc.HasValue;
        public bool HasEvictionNotice => EvictionNoticeIssuedAtUtc.HasValue;
        public bool IsEvictionEligible => EvictionEligibleAtUtc.HasValue;

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

        public int ResolveDueInstallmentCount(DateTimeOffset asOfUtc)
        {
            if (!IsActive)
                return 0;

            DateTimeOffset dueAnchor = FirstMissedChargeDueAtUtc ?? NextChargeDueAtUtc;
            if (dueAnchor > asOfUtc)
                return 0;

            return BillingCadence switch
            {
                CityHouseholdObligationBillingCadence.Daily => Math.Max(
                    val1: 1,
                    val2: (asOfUtc.UtcDateTime.Date - dueAnchor.UtcDateTime.Date).Days + 1),
                CityHouseholdObligationBillingCadence.Weekly => Math.Max(
                    val1: 1,
                    val2: ((asOfUtc.UtcDateTime.Date - dueAnchor.UtcDateTime.Date).Days / 7) + 1),
                _ => ResolveMonthlyDueInstallmentCount(
                    dueAnchor: dueAnchor,
                    asOfUtc: asOfUtc)
            };
        }

        public int ResolveDelinquentBillingCycles(DateTimeOffset asOfUtc)
        {
            return !HasActiveDelinquency
                ? 0
                : ResolveDueInstallmentCount(asOfUtc);
        }

        public int ResolveDelinquencyAgeDays(DateTimeOffset asOfUtc)
        {
            if (!FirstMissedChargeDueAtUtc.HasValue)
                return 0;

            return Math.Max(
                val1: 0,
                val2: (asOfUtc.UtcDateTime.Date - FirstMissedChargeDueAtUtc.Value.UtcDateTime.Date).Days);
        }

        public Money ResolveCurrentDueAmount(DateTimeOffset asOfUtc)
        {
            int installmentCount = ResolveDueInstallmentCount(asOfUtc);
            return installmentCount <= 0
                ? Money.Zero
                : ChargeAmount.Multiply(installmentCount);
        }

        public Money ResolveCurrentDueTaxAmount(DateTimeOffset asOfUtc)
        {
            int installmentCount = ResolveDueInstallmentCount(asOfUtc);
            return installmentCount <= 0
                ? Money.Zero
                : TaxAmount.Multiply(installmentCount);
        }

        public void MarkCharged(DateTimeOffset chargedAtUtc)
        {
            if (!IsActive)
                throw new InvalidOperationException("Cannot charge an inactive obligation.");

            int settledInstallmentCount = ResolveDueInstallmentCount(chargedAtUtc);
            if (settledInstallmentCount <= 0)
                settledInstallmentCount = 1;

            DateTimeOffset dueAnchor = FirstMissedChargeDueAtUtc ?? NextChargeDueAtUtc;
            LastChargedAtUtc = chargedAtUtc;
            ChargeCount += settledInstallmentCount;
            NextChargeDueAtUtc = AddCadence(
                value: dueAnchor,
                periods: settledInstallmentCount);
            ResetDelinquency();
        }

        public void MarkChargeMissed(DateTimeOffset attemptedAtUtc)
        {
            if (!IsActive)
                throw new InvalidOperationException("Cannot miss an inactive obligation charge.");

            if (LastChargeAttemptedAtUtc.HasValue &&
                LastChargeAttemptedAtUtc.Value.UtcDateTime.Date == attemptedAtUtc.UtcDateTime.Date)
            {
                LastChargeAttemptedAtUtc = attemptedAtUtc;
                ApplyDelinquencyEscalation(attemptedAtUtc);
                return;
            }

            LastChargeAttemptedAtUtc = attemptedAtUtc;
            FirstMissedChargeDueAtUtc ??= NextChargeDueAtUtc <= attemptedAtUtc
                ? NextChargeDueAtUtc
                : attemptedAtUtc;
            MissedChargeCount++;
            ApplyDelinquencyEscalation(attemptedAtUtc);
        }

        public bool IsDue(DateTimeOffset asOfUtc)
        {
            return IsActive && NextChargeDueAtUtc <= asOfUtc;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public void Reprice(decimal multiplier)
        {
            if (multiplier is <= 0m or > 3m)
                throw new ArgumentOutOfRangeException(nameof(multiplier));

            ChargeAmount = BaseChargeAmount.Multiply(multiplier);
            TaxAmount = BaseTaxAmount.Multiply(multiplier);
        }

        private void ResetDelinquency()
        {
            LastChargeAttemptedAtUtc = null;
            FirstMissedChargeDueAtUtc = null;
            ServiceCutoffAtUtc = null;
            EvictionNoticeIssuedAtUtc = null;
            EvictionEligibleAtUtc = null;
            MissedChargeCount = 0;
        }

        private void ApplyDelinquencyEscalation(DateTimeOffset asOfUtc)
        {
            int delinquencyAgeDays = ResolveDelinquencyAgeDays(asOfUtc);
            int delinquentBillingCycles = ResolveDelinquentBillingCycles(asOfUtc);

            switch (Kind)
            {
                case CityHouseholdObligationKind.Utilities:
                    if (!ServiceCutoffAtUtc.HasValue &&
                        (delinquentBillingCycles >= 2 || delinquencyAgeDays >= 21))
                        ServiceCutoffAtUtc = asOfUtc;
                    break;

                case CityHouseholdObligationKind.Rent:
                    if (!EvictionNoticeIssuedAtUtc.HasValue &&
                        (delinquentBillingCycles >= 2 || delinquencyAgeDays >= 35))
                        EvictionNoticeIssuedAtUtc = asOfUtc;

                    if (!EvictionEligibleAtUtc.HasValue &&
                        (delinquentBillingCycles >= 3 || delinquencyAgeDays >= 60))
                        EvictionEligibleAtUtc = asOfUtc;
                    break;
            }
        }

        private int ResolveMonthlyDueInstallmentCount(
            DateTimeOffset dueAnchor,
            DateTimeOffset asOfUtc)
        {
            int installmentCount = 1;
            DateTimeOffset probe = dueAnchor;

            while (AddCadence(
                       value: probe,
                       periods: 1) <=
                   asOfUtc)
            {
                probe = AddCadence(
                    value: probe,
                    periods: 1);
                installmentCount++;
            }

            return installmentCount;
        }

        private DateTimeOffset AddCadence(
            DateTimeOffset value,
            int periods)
        {
            return BillingCadence switch
            {
                CityHouseholdObligationBillingCadence.Daily => value.AddDays(periods),
                CityHouseholdObligationBillingCadence.Weekly => value.AddDays(7 * periods),
                _ => value.AddMonths(periods)
            };
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
