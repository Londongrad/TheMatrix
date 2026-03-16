using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Services;
using Matrix.Economy.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Consumers
{
    public sealed class CityEconomyDailySettlementConsumer(
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository ledgerRepository,
        ICityBudgetSettlementRepository settlementRepository,
        IEconomyUnitOfWork unitOfWork,
        CityBudgetOperatingExpensePolicy operatingExpensePolicy,
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
                ?? throw CreateMissingBudgetException(message.CityId);

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

            var operatingExpense = operatingExpensePolicy.Build(settlement);

            budget.ApplySettlement(
                settlement: settlement,
                operatingExpense: operatingExpense);
            await AddLedgerEntryIfPositiveAsync(
                ledgerRepository: ledgerRepository,
                cityId: message.CityId,
                kind: CityBudgetLedgerEntryKind.Revenue,
                category: CityBudgetCategory.Taxation,
                amount: Money.FromDecimal(message.IncomeTaxAmount),
                title: "Income tax settlement",
                description: "Resident payroll income tax transferred into the city budget.",
                referenceCode: $"{message.CorrelationId}:income-tax",
                cancellationToken: context.CancellationToken);
            await AddLedgerEntryIfPositiveAsync(
                ledgerRepository: ledgerRepository,
                cityId: message.CityId,
                kind: CityBudgetLedgerEntryKind.Revenue,
                category: CityBudgetCategory.Commerce,
                amount: Money.FromDecimal(message.RetailTaxAmount),
                title: "Retail tax settlement",
                description: "Household retail turnover tax transferred into the city budget.",
                referenceCode: $"{message.CorrelationId}:retail-tax",
                cancellationToken: context.CancellationToken);
            await AddLedgerEntryIfPositiveAsync(
                ledgerRepository: ledgerRepository,
                cityId: message.CityId,
                kind: CityBudgetLedgerEntryKind.Expense,
                category: CityBudgetCategory.Operations,
                amount: operatingExpense.TotalExpense,
                title: "Municipal operating expense",
                description: "Budget allocation for baseline city upkeep and operating services.",
                referenceCode: $"{message.CorrelationId}:operations",
                cancellationToken: context.CancellationToken);
            await settlementRepository.AddAsync(settlement, context.CancellationToken);
            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                "Applied city economy settlement for cityId={CityId}, tickId={TickId}, incomeTax={IncomeTax}, retailTax={RetailTax}, cityExpense={CityExpense}.",
                message.CityId,
                message.TickId,
                message.IncomeTaxAmount,
                message.RetailTaxAmount,
                operatingExpense.TotalExpense.Amount);
        }

        private static InvalidOperationException CreateMissingBudgetException(Guid cityId)
        {
            return new InvalidOperationException(
                $"Economy budget for city '{cityId}' is not initialized yet. Daily city settlement requires CityCreatedV1 to be processed first.");
        }

        private static CityBudgetLedgerEntry CreateLedgerEntry(
            Guid cityId,
            CityBudgetLedgerEntryKind kind,
            CityBudgetCategory category,
            Money amount,
            string title,
            string description,
            string referenceCode)
        {
            return new CityBudgetLedgerEntry(
                id: Guid.NewGuid(),
                cityId: cityId,
                occurredAtUtc: DateTimeOffset.UtcNow,
                kind: kind,
                category: category,
                amount: amount,
                title: title,
                description: description,
                source: CityBudgetLedgerEntrySource.Settlement,
                referenceCode: referenceCode);
        }

        private static Task AddLedgerEntryIfPositiveAsync(
            ICityBudgetLedgerRepository ledgerRepository,
            Guid cityId,
            CityBudgetLedgerEntryKind kind,
            CityBudgetCategory category,
            Money amount,
            string title,
            string description,
            string referenceCode,
            CancellationToken cancellationToken)
        {
            return amount.IsPositive
                ? ledgerRepository.AddAsync(
                    CreateLedgerEntry(
                        cityId: cityId,
                        kind: kind,
                        category: category,
                        amount: amount,
                        title: title,
                        description: description,
                        referenceCode: referenceCode),
                    cancellationToken)
                : Task.CompletedTask;
        }
    }
}
