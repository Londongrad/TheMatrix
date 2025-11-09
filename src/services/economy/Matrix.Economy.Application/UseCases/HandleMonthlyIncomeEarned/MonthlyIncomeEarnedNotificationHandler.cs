using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HandleMonthlyIncomeEarned
{
    public sealed class MonthlyIncomeEarnedNotificationHandler(
        ICityBudgetRepository budgetRepository,
        IEconomyUnitOfWork unitOfWork)
                : INotificationHandler<MonthlyIncomeEarnedNotification>
    {
        private readonly ICityBudgetRepository _budgetRepository = budgetRepository;
        private readonly IEconomyUnitOfWork _unitOfWork = unitOfWork;

        public async Task Handle(MonthlyIncomeEarnedNotification notification, CancellationToken cancellationToken)
        {
            var msg = notification.Message;

            // 1. Загружаем текущий бюджет или создаем, если его нет
            var budget = await _budgetRepository.GetCurrentAsync(cancellationToken);

            if (budget is null)
            {
                budget = new CityBudget(CityBudgetId.New());
                _budgetRepository.Add(budget);
            }

            // 2. Конвертим decimal налог в Money
            var taxMoney = new Money(msg.TaxAmount);

            // 3. Регистрируем налоговый доход
            budget.RegisterTaxIncome(
                taxMoney,
                msg.SimulationMonth,
                source: $"Population.Person:{msg.PersonId}",
                correlationId: msg.CorrelationId);

            // 4. Сохраняем
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
