using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;

namespace Matrix.Economy.Domain.Aggregates
{
    public sealed class CityBudget
    {
        public CityBudgetId Id { get; }
        public Guid CityId { get; private set; }
        public CityBudgetUnitKind UnitKind { get; private set; }
        public string UnitCode { get; private set; } = string.Empty;
        public string UnitDisplayName { get; private set; } = string.Empty;
        public string UnitSymbol { get; private set; } = string.Empty;
        public Money Balance { get; private set; } = null!;
        public Money TotalTaxIncome { get; private set; } = null!;
        public Money TotalIncomeTaxIncome { get; private set; } = null!;
        public Money TotalSalesTaxIncome { get; private set; } = null!;
        public Money TotalDirectRevenue { get; private set; } = null!;
        public Money TotalCityExpenses { get; private set; } = null!;
        public Money TotalRetailTurnover { get; private set; } = null!;
        public Money TotalGrossPayroll { get; private set; } = null!;
        public Money TotalNetPayroll { get; private set; } = null!;

        private CityBudget()
        {
        }

        public CityBudget(CityBudgetId id, Guid cityId)
            : this(id, cityId, CityBudgetUnitProfile.DefaultMoney())
        {
        }

        public CityBudget(
            CityBudgetId id,
            Guid cityId,
            CityBudgetUnitProfile unitProfile)
        {
            Id = id;
            CityId = cityId;
            ApplyUnitProfile(unitProfile);
            Balance = Money.Zero;
            TotalTaxIncome = Money.Zero;
            TotalIncomeTaxIncome = Money.Zero;
            TotalSalesTaxIncome = Money.Zero;
            TotalDirectRevenue = Money.Zero;
            TotalCityExpenses = Money.Zero;
            TotalRetailTurnover = Money.Zero;
            TotalGrossPayroll = Money.Zero;
            TotalNetPayroll = Money.Zero;
        }

        public void ApplySettlement(
            CityBudgetSettlement settlement,
            CityBudgetOperatingExpenseProfile operatingExpense)
        {
            if (settlement.CityId != CityId)
                throw new InvalidOperationException("Settlement city does not match budget city.");

            Money totalTax = settlement.IncomeTax.Add(settlement.RetailTax);
            TotalIncomeTaxIncome = TotalIncomeTaxIncome.Add(settlement.IncomeTax);
            TotalSalesTaxIncome = TotalSalesTaxIncome.Add(settlement.RetailTax);
            TotalTaxIncome = TotalTaxIncome.Add(totalTax);
            TotalCityExpenses = TotalCityExpenses.Add(operatingExpense.TotalExpense);
            TotalRetailTurnover = TotalRetailTurnover.Add(settlement.RetailTurnover);
            TotalGrossPayroll = TotalGrossPayroll.Add(settlement.GrossPayroll);
            TotalNetPayroll = TotalNetPayroll.Add(settlement.NetPayroll);
            Balance = Balance.Add(totalTax).Subtract(operatingExpense.TotalExpense);
        }

        public void ApplyLedgerEntry(CityBudgetLedgerEntry entry)
        {
            if (entry.CityId != CityId)
                throw new InvalidOperationException("Ledger entry city does not match budget city.");

            switch (entry.Kind)
            {
                case CityBudgetLedgerEntryKind.Revenue:
                    TotalDirectRevenue = TotalDirectRevenue.Add(entry.Amount);
                    Balance = Balance.Add(entry.Amount);
                    break;
                case CityBudgetLedgerEntryKind.Expense:
                    TotalCityExpenses = TotalCityExpenses.Add(entry.Amount);
                    Balance = Balance.Subtract(entry.Amount);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported ledger entry kind '{entry.Kind}'.");
            }
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
                    $"Budget unit mismatch. Existing={UnitKind}:{UnitCode}, requested={requestedUnitProfile.Kind}:{requestedUnitProfile.Code}.");
            }
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
