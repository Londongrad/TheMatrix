using Matrix.Economy.Application.Messages;
using MediatR;

namespace Matrix.Economy.Application.UseCases.HandleMonthlyIncomeEarned
{
    /// <summary>
    /// Notification, который будет публиковаться инфраструктурой
    /// при получении сообщения от брокера.
    /// </summary>
    public sealed record MonthlyIncomeEarnedNotification(MonthlyIncomeEarnedMessage Message) : INotification;
}
