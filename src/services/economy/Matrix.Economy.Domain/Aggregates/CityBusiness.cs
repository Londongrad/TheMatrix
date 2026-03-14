using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;

namespace Matrix.Economy.Domain.Aggregates
{
    public sealed class CityBusiness
    {
        public Guid Id { get; private set; }
        public Guid CityId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string? TemplateKey { get; private set; }
        public CityBusinessKind Kind { get; private set; }
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

        private CityBusiness()
        {
        }

        public CityBusiness(
            Guid id,
            Guid cityId,
            string name,
            string? templateKey,
            CityBusinessKind kind,
            DateTimeOffset createdAtUtc,
            CityBudgetUnitProfile unitProfile,
            Money initialCapital)
        {
            Id = GuardHelper.AgainstEmptyGuid(id, nameof(id));
            CityId = GuardHelper.AgainstEmptyGuid(cityId, nameof(cityId));
            Name = string.IsNullOrWhiteSpace(name)
                ? throw new ArgumentException("Business name is required.", nameof(name))
                : name.Trim();
            TemplateKey = string.IsNullOrWhiteSpace(templateKey)
                ? null
                : templateKey.Trim();
            Kind = kind;
            CreatedAtUtc = createdAtUtc;
            ApplyUnitProfile(unitProfile);

            if (initialCapital.IsNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapital), "Initial capital cannot be negative.");
            }

            Balance = initialCapital;
            TaxReserve = Money.Zero;
            TotalCapitalInjections = initialCapital;
            TotalRetailTurnover = Money.Zero;
            TotalNetSalesRevenue = Money.Zero;
            TotalOperatingExpenses = Money.Zero;
            TotalTaxRemitted = Money.Zero;
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
                    $"Business unit mismatch. Existing={UnitKind}:{UnitCode}, requested={requestedUnitProfile.Kind}:{requestedUnitProfile.Code}.");
            }
        }

        public void EnsureCanIssuePayroll()
        {
            if (Kind is CityBusinessKind.Generic
                or CityBusinessKind.Employer
                or CityBusinessKind.Service
                or CityBusinessKind.Manufacturer
                or CityBusinessKind.Utility
                or CityBusinessKind.MunicipalVendor)
            {
                return;
            }

            throw new InvalidOperationException($"Business kind '{Kind}' cannot issue payroll.");
        }

        public void EnsureCanRecordConsumerSale()
        {
            if (Kind is CityBusinessKind.Generic
                or CityBusinessKind.RetailStore
                or CityBusinessKind.Service
                or CityBusinessKind.Utility
                or CityBusinessKind.MunicipalVendor)
            {
                return;
            }

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
            {
                throw new InvalidOperationException(
                    $"Business kind '{Kind}' cannot serve obligation kind '{obligationKind}'.");
            }
        }

        public void InjectCapital(Money amount)
        {
            if (!amount.IsPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Capital injection must be positive.");
            }

            Balance = Balance.Add(amount);
            TotalCapitalInjections = TotalCapitalInjections.Add(amount);
        }

        public void RecordRetailSale(Money grossAmount, Money salesTaxAmount)
        {
            if (!grossAmount.IsPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(grossAmount), "Retail sale amount must be positive.");
            }

            if (salesTaxAmount.IsNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(salesTaxAmount), "Sales tax amount cannot be negative.");
            }

            if (salesTaxAmount.Amount > grossAmount.Amount)
            {
                throw new InvalidOperationException("Sales tax cannot exceed gross sale amount.");
            }

            Money netRevenue = grossAmount.Subtract(salesTaxAmount);
            Balance = Balance.Add(grossAmount);
            TaxReserve = TaxReserve.Add(salesTaxAmount);
            TotalRetailTurnover = TotalRetailTurnover.Add(grossAmount);
            TotalNetSalesRevenue = TotalNetSalesRevenue.Add(netRevenue);
        }

        public void RecordObligationRevenue(Money grossAmount, Money salesTaxAmount)
        {
            RecordRetailSale(grossAmount, salesTaxAmount);
        }

        public void RecordSettledRetailSale(Money grossAmount, Money salesTaxAmount)
        {
            if (!grossAmount.IsPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(grossAmount), "Retail sale amount must be positive.");
            }

            if (salesTaxAmount.IsNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(salesTaxAmount), "Sales tax amount cannot be negative.");
            }

            if (salesTaxAmount.Amount > grossAmount.Amount)
            {
                throw new InvalidOperationException("Sales tax cannot exceed gross sale amount.");
            }

            Money netRevenue = grossAmount.Subtract(salesTaxAmount);
            Balance = Balance.Add(netRevenue);
            TotalRetailTurnover = TotalRetailTurnover.Add(grossAmount);
            TotalNetSalesRevenue = TotalNetSalesRevenue.Add(netRevenue);
        }

        public void RecordMunicipalRevenue(Money amount)
        {
            if (!amount.IsPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Municipal revenue must be positive.");
            }

            Balance = Balance.Add(amount);
        }

        public void RecordOperatingExpense(Money amount)
        {
            if (!amount.IsPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Operating expense must be positive.");
            }

            Balance = Balance.Subtract(amount);
            TotalOperatingExpenses = TotalOperatingExpenses.Add(amount);
        }

        public void RemitTax(Money amount)
        {
            if (!amount.IsPositive)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Tax remittance must be positive.");
            }

            if (amount.Amount > TaxReserve.Amount)
            {
                throw new InvalidOperationException("Cannot remit more tax than the current reserve.");
            }

            Balance = Balance.Subtract(amount);
            TaxReserve = TaxReserve.Subtract(amount);
            TotalTaxRemitted = TotalTaxRemitted.Add(amount);
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
