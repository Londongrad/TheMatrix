using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates
{
    public sealed class CityBusiness
    {
        private CityBusiness() { }

        public CityBusiness(
            Guid id,
            Guid cityId,
            string name,
            string? externalReferenceCode,
            string? templateKey,
            CityBusinessKind kind,
            DateTimeOffset createdAtUtc,
            CityBudgetUnitProfile unitProfile,
            Money initialCapital)
        {
            Id = GuardHelper.AgainstEmptyGuid(
                id: id,
                propertyName: nameof(id));
            CityId = GuardHelper.AgainstEmptyGuid(
                id: cityId,
                propertyName: nameof(cityId));
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException(
                    message: "Business name is required.",
                    paramName: nameof(name))
                : name.Trim();
            ExternalReferenceCode = string.IsNullOrWhiteSpace(externalReferenceCode)
                ? null
                : externalReferenceCode.Trim();
            TemplateKey = string.IsNullOrWhiteSpace(templateKey)
                ? null
                : templateKey.Trim();
            Kind = kind;
            CreatedAtUtc = createdAtUtc;
            ApplyUnitProfile(unitProfile);

            if (initialCapital.IsNegative)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(initialCapital),
                    message: "Initial capital cannot be negative.");

            Balance = initialCapital;
            TaxReserve = Money.Zero;
            TotalCapitalInjections = initialCapital;
            TotalRetailTurnover = Money.Zero;
            TotalNetSalesRevenue = Money.Zero;
            TotalOperatingExpenses = Money.Zero;
            TotalTaxRemitted = Money.Zero;
        }

        public Guid Id { get; private set; }
        public Guid CityId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string? ExternalReferenceCode { get; private set; }
        public string? TemplateKey { get; private set; }
        public CityBusinessKind Kind { get; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public CityBudgetUnitKind UnitKind { get; private set; }
        public string UnitCode { get; private set; } = string.Empty;
        public string UnitDisplayName { get; private set; } = string.Empty;
        public string UnitSymbol { get; private set; } = string.Empty;
        public Money Balance { get; private set; } = null!;
        public Money TaxReserve { get; private set; } = null!;
        public Money TotalCapitalInjections { get; private set; } = null!;
        public Money TotalRetailTurnover { get; private set; } = null!;
        public Money TotalNetSalesRevenue { get; private set; } = null!;
        public Money TotalOperatingExpenses { get; private set; } = null!;
        public Money TotalTaxRemitted { get; private set; } = null!;

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
                    $"Business unit mismatch. Existing={UnitKind}:{UnitCode}, requested={requestedUnitProfile.Kind}:{requestedUnitProfile.Code}.");
        }

        public void EnsureCanIssuePayroll()
        {
            if (Kind is CityBusinessKind.Generic
             or CityBusinessKind.Employer
             or CityBusinessKind.Service
             or CityBusinessKind.Manufacturer
             or CityBusinessKind.Utility
             or CityBusinessKind.MunicipalVendor)
                return;

            throw new InvalidOperationException($"Business kind '{Kind}' cannot issue payroll.");
        }

        public void EnsureCanRecordConsumerSale()
        {
            if (Kind is CityBusinessKind.Generic
             or CityBusinessKind.RetailStore
             or CityBusinessKind.Service
             or CityBusinessKind.Utility
             or CityBusinessKind.MunicipalVendor)
                return;

            throw new InvalidOperationException($"Business kind '{Kind}' cannot record consumer sales.");
        }

        public void EnsureCanServeObligation(CityHouseholdObligationKind obligationKind)
        {
            bool allowed = obligationKind switch
            {
                CityHouseholdObligationKind.Rent => Kind is CityBusinessKind.Generic
                 or CityBusinessKind.Landlord
                 or CityBusinessKind.MunicipalVendor,
                CityHouseholdObligationKind.Utilities => Kind is CityBusinessKind.Generic
                 or CityBusinessKind.Utility
                 or CityBusinessKind.MunicipalVendor,
                CityHouseholdObligationKind.ServiceFee => Kind is CityBusinessKind.Generic
                 or CityBusinessKind.Service
                 or CityBusinessKind.MunicipalVendor,
                _ => false
            };

            if (!allowed)
                throw new InvalidOperationException(
                    $"Business kind '{Kind}' cannot serve obligation kind '{obligationKind}'.");
        }

        public void InjectCapital(Money amount)
        {
            if (!amount.IsPositive)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(amount),
                    message: "Capital injection must be positive.");

            Balance = Balance.Add(amount);
            TotalCapitalInjections = TotalCapitalInjections.Add(amount);
        }

        public void RecordRetailSale(
            Money grossAmount,
            Money salesTaxAmount)
        {
            if (!grossAmount.IsPositive)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(grossAmount),
                    message: "Retail sale amount must be positive.");

            if (salesTaxAmount.IsNegative)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(salesTaxAmount),
                    message: "Sales tax amount cannot be negative.");

            if (salesTaxAmount.Amount > grossAmount.Amount)
                throw new InvalidOperationException("Sales tax cannot exceed gross sale amount.");

            Money netRevenue = grossAmount.Subtract(salesTaxAmount);
            Balance = Balance.Add(grossAmount);
            TaxReserve = TaxReserve.Add(salesTaxAmount);
            TotalRetailTurnover = TotalRetailTurnover.Add(grossAmount);
            TotalNetSalesRevenue = TotalNetSalesRevenue.Add(netRevenue);
        }

        public void RecordObligationRevenue(
            Money grossAmount,
            Money salesTaxAmount)
        {
            RecordRetailSale(
                grossAmount: grossAmount,
                salesTaxAmount: salesTaxAmount);
        }

        public void RecordSettledRetailSale(
            Money grossAmount,
            Money salesTaxAmount)
        {
            if (!grossAmount.IsPositive)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(grossAmount),
                    message: "Retail sale amount must be positive.");

            if (salesTaxAmount.IsNegative)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(salesTaxAmount),
                    message: "Sales tax amount cannot be negative.");

            if (salesTaxAmount.Amount > grossAmount.Amount)
                throw new InvalidOperationException("Sales tax cannot exceed gross sale amount.");

            Money netRevenue = grossAmount.Subtract(salesTaxAmount);
            Balance = Balance.Add(netRevenue);
            TotalRetailTurnover = TotalRetailTurnover.Add(grossAmount);
            TotalNetSalesRevenue = TotalNetSalesRevenue.Add(netRevenue);
        }

        public void RecordMunicipalRevenue(Money amount)
        {
            if (!amount.IsPositive)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(amount),
                    message: "Municipal revenue must be positive.");

            Balance = Balance.Add(amount);
        }

        public void RecordOperatingExpense(Money amount)
        {
            if (!amount.IsPositive)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(amount),
                    message: "Operating expense must be positive.");

            Balance = Balance.Subtract(amount);
            TotalOperatingExpenses = TotalOperatingExpenses.Add(amount);
        }

        public CityBusinessPayrollSettlementOutcome SettlePayroll(
            Money requestedGrossPayroll,
            Money requestedIncomeTax)
        {
            if (!requestedGrossPayroll.IsPositive)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(requestedGrossPayroll),
                    message: "Requested payroll must be positive.");

            if (requestedIncomeTax.IsNegative || requestedIncomeTax.Amount > requestedGrossPayroll.Amount)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(requestedIncomeTax),
                    message: "Requested income tax must be between zero and requested gross payroll.");

            decimal availableBalanceAmount = Math.Max(
                val1: 0m,
                val2: Balance.Amount);
            decimal paidGrossAmount = Math.Min(
                val1: requestedGrossPayroll.Amount,
                val2: availableBalanceAmount);

            if (paidGrossAmount <= 0m)
                return new CityBusinessPayrollSettlementOutcome(
                    RequestedGrossPayroll: requestedGrossPayroll,
                    RequestedIncomeTax: requestedIncomeTax,
                    PaidGrossPayroll: Money.Zero,
                    PaidIncomeTax: Money.Zero,
                    PaidNetPayroll: Money.Zero,
                    GrossShortfall: requestedGrossPayroll,
                    FulfillmentRatio: 0m);

            decimal fulfillmentRatio = requestedGrossPayroll.Amount <= 0m
                ? 0m
                : paidGrossAmount / requestedGrossPayroll.Amount;
            decimal paidIncomeTaxAmount = Math.Min(
                val1: paidGrossAmount,
                val2: decimal.Round(
                    d: requestedIncomeTax.Amount * fulfillmentRatio,
                    decimals: 2,
                    mode: MidpointRounding.AwayFromZero));
            var paidGrossPayroll = Money.FromDecimal(paidGrossAmount);
            var paidIncomeTax = Money.FromDecimal(paidIncomeTaxAmount);
            Money paidNetPayroll = paidGrossPayroll.Subtract(paidIncomeTax);
            Money grossShortfall = requestedGrossPayroll.Subtract(paidGrossPayroll);

            RecordOperatingExpense(paidGrossPayroll);

            return new CityBusinessPayrollSettlementOutcome(
                RequestedGrossPayroll: requestedGrossPayroll,
                RequestedIncomeTax: requestedIncomeTax,
                PaidGrossPayroll: paidGrossPayroll,
                PaidIncomeTax: paidIncomeTax,
                PaidNetPayroll: paidNetPayroll,
                GrossShortfall: grossShortfall,
                FulfillmentRatio: decimal.Round(
                    d: fulfillmentRatio,
                    decimals: 4,
                    mode: MidpointRounding.AwayFromZero));
        }

        public void RemitTax(Money amount)
        {
            if (!amount.IsPositive)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(amount),
                    message: "Tax remittance must be positive.");

            if (amount.Amount > TaxReserve.Amount)
                throw new InvalidOperationException("Cannot remit more tax than the current reserve.");

            Balance = Balance.Subtract(amount);
            TaxReserve = TaxReserve.Subtract(amount);
            TotalTaxRemitted = TotalTaxRemitted.Add(amount);
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
