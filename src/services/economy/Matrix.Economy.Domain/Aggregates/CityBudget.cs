using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Domain.ValueObjects;

namespace Matrix.Economy.Domain.Aggregates
{
    /// <summary>
    /// Городской бюджет (пока очень упрощённый).
    /// </summary>
    public sealed class CityBudget
    {
        public CityBudgetId Id { get; }

        /// <summary> Общий баланс бюджета (доходы - расходы). </summary>
        public Money Balance { get; private set; } = null!;

        /// <summary> Всего налогов собрано за всё время. </summary>
        public Money TotalTaxIncome { get; private set; } = null!;

        // позже добавим TotalExpenses, TotalSalaries, TotalMaintenance и т.п.

        private CityBudget()
        {
        }

        public CityBudget(CityBudgetId id)
        {
            Id = id;
            Balance = Money.Zero;
            TotalTaxIncome = Money.Zero;
        }

        /// <summary>
        /// Регистрирует налоговый доход (например, от MonthlyIncome из Population).
        /// </summary>
        public void RegisterTaxIncome(Money taxAmount, int simulationMonth, string source, string correlationId)
        {
            // здесь можно сделать доменные проверки (например, simulationMonth > 0)

            TotalTaxIncome = TotalTaxIncome.Add(taxAmount);
            Balance = Balance.Add(taxAmount);

            // позже: добавить запись в ledger/transactions
        }
    }
}
