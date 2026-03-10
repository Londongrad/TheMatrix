using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;

namespace Matrix.Economy.Domain.Aggregates
{
    /// <summary>
    /// Городской бюджет (пока очень упрощённый).
    /// </summary>
    public sealed class CityBudget
    {
        public CityBudgetId Id { get; }
        public Guid CityId { get; private set; }

        /// <summary> Общий баланс бюджета (доходы - расходы). </summary>
        public Money Balance { get; private set; } = null!;

        /// <summary> Всего налогов собрано за всё время. </summary>
        public Money TotalTaxIncome { get; private set; } = null!;
        public Money TotalIncomeTaxIncome { get; private set; } = null!;
        public Money TotalSalesTaxIncome { get; private set; } = null!;
        public Money TotalCityExpenses { get; private set; } = null!;
        public Money TotalRetailTurnover { get; private set; } = null!;
        public Money TotalGrossPayroll { get; private set; } = null!;
        public Money TotalNetPayroll { get; private set; } = null!;

        private CityBudget()
        {
        }

        public CityBudget(CityBudgetId id, Guid cityId)
        {
            Id = id;
            CityId = cityId;
            Balance = Money.Zero;
            TotalTaxIncome = Money.Zero;
            TotalIncomeTaxIncome = Money.Zero;
            TotalSalesTaxIncome = Money.Zero;
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
    }
}
