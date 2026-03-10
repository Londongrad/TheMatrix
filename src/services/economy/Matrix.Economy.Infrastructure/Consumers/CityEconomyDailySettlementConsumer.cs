using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class CityEconomyDailySettlementConsumer(
        ICityBudgetRepository budgetRepository,
        ICityBudgetSettlementRepository settlementRepository,
        IEconomyUnitOfWork unitOfWork,
        ILogger<CityEconomyDailySettlementConsumer> logger)
        : IConsumer<CityEconomyDailySettlementV1>
    {
        public async Task Consume(ConsumeContext<CityEconomyDailySettlementV1> context)
        {
            CityEconomyDailySettlementV1 message = context.Message;

            if (await settlementRepository.ExistsAsync(message.CityId, message.TickId, context.CancellationToken))
            {
                logger.LogDebug(
                    "Skipped duplicate city economy settlement for cityId={CityId}, tickId={TickId}.",
                    message.CityId,
                    message.TickId);
                return;
            }

            CityBudget budget = await budgetRepository.GetByCityAsync(message.CityId, context.CancellationToken)
                ?? CreateBudget(message.CityId, budgetRepository);

            var settlement = new CityBudgetSettlement(
                id: Guid.NewGuid(),
                cityId: message.CityId,
                tickId: message.TickId,
                currentDate: message.CurrentDate,
                settledDays: message.SettledDays,
                householdCount: message.HouseholdCount,
                residentCount: message.ResidentCount,
                grossPayroll: Money.FromDecimal(message.GrossPayrollAmount),
                incomeTax: Money.FromDecimal(message.IncomeTaxAmount),
                netPayroll: Money.FromDecimal(message.NetPayrollAmount),
                retailTurnover: Money.FromDecimal(message.RetailTurnoverAmount),
                retailTax: Money.FromDecimal(message.RetailTaxAmount),
                housingSpend: Money.FromDecimal(message.HousingSpendAmount),
                correlationId: message.CorrelationId,
                occurredAtUtc: message.OccurredAtUtc);

            budget.ApplySettlement(settlement);
            await settlementRepository.AddAsync(settlement, context.CancellationToken);
            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                "Applied city economy settlement for cityId={CityId}, tickId={TickId}, incomeTax={IncomeTax}, retailTax={RetailTax}.",
                message.CityId,
                message.TickId,
                message.IncomeTaxAmount,
                message.RetailTaxAmount);
        }

        private static CityBudget CreateBudget(Guid cityId, ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(CityBudgetId.New(), cityId);
            budgetRepository.Add(budget);
            return budget;
        }
    }
}
