using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Models;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Services;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class CityEconomyDailySettlementConsumer(
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository ledgerRepository,
        ICityBudgetSettlementRepository settlementRepository,
        ICityEconomyDeletionRepository deletionRepository,
        IEconomyUnitOfWork unitOfWork,
        CityBudgetOperatingExpensePolicy operatingExpensePolicy,
        TimeProvider timeProvider,
        ILogger<CityEconomyDailySettlementConsumer> logger)
        : IConsumer<CityEconomyDailySettlementV1>
    {
        public async Task Consume(ConsumeContext<CityEconomyDailySettlementV1> context)
        {
            await ConsumeAsync(
                message: context.Message,
                cancellationToken: context.CancellationToken);
        }

        internal async Task ConsumeAsync(
            CityEconomyDailySettlementV1 message,
            CancellationToken cancellationToken)
        {
            if (await deletionRepository.GetDeletedAtUtcAsync(
                    cityId: message.CityId,
                    cancellationToken: cancellationToken) is not null)
            {
                logger.LogDebug(
                    message: "Skipped economy settlement for deleted cityId={CityId}, tickId={TickId}.",
                    message.CityId,
                    message.TickId);
                return;
            }

            if (await settlementRepository.ExistsAsync(
                    cityId: message.CityId,
                    tickId: message.TickId,
                    cancellationToken: cancellationToken))
            {
                logger.LogDebug(
                    message: "Skipped duplicate city economy settlement for cityId={CityId}, tickId={TickId}.",
                    message.CityId,
                    message.TickId);
                return;
            }

            CityBudget budget = await CityBudgetInitializationSupport.EnsureExistsAsync(
                cityId: message.CityId,
                budgetRepository: budgetRepository,
                unitOfWork: unitOfWork,
                cancellationToken: cancellationToken);

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

            CityBudgetOperatingExpenseProfile operatingExpense = operatingExpensePolicy.Build(settlement);

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
                cancellationToken: cancellationToken);
            await AddLedgerEntryIfPositiveAsync(
                ledgerRepository: ledgerRepository,
                cityId: message.CityId,
                kind: CityBudgetLedgerEntryKind.Revenue,
                category: CityBudgetCategory.Commerce,
                amount: Money.FromDecimal(message.RetailTaxAmount),
                title: "Retail tax settlement",
                description: "Household retail turnover tax transferred into the city budget.",
                referenceCode: $"{message.CorrelationId}:retail-tax",
                cancellationToken: cancellationToken);
            await AddLedgerEntryIfPositiveAsync(
                ledgerRepository: ledgerRepository,
                cityId: message.CityId,
                kind: CityBudgetLedgerEntryKind.Expense,
                category: CityBudgetCategory.Operations,
                amount: operatingExpense.TotalExpense,
                title: "Municipal operating expense",
                description: "Budget allocation for baseline city upkeep and operating services.",
                referenceCode: $"{message.CorrelationId}:operations",
                cancellationToken: cancellationToken);
            await settlementRepository.AddAsync(
                settlement: settlement,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                message:
                "Applied city economy settlement for cityId={CityId}, tickId={TickId}, incomeTax={IncomeTax}, retailTax={RetailTax}, cityExpense={CityExpense}.",
                message.CityId,
                message.TickId,
                message.IncomeTaxAmount,
                message.RetailTaxAmount,
                operatingExpense.TotalExpense.Amount);
        }

        private CityBudgetLedgerEntry CreateLedgerEntry(
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
                occurredAtUtc: timeProvider.GetUtcNow(),
                kind: kind,
                category: category,
                amount: amount,
                title: title,
                description: description,
                source: CityBudgetLedgerEntrySource.Settlement,
                referenceCode: referenceCode);
        }

        private Task AddLedgerEntryIfPositiveAsync(
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
                    entry: CreateLedgerEntry(
                        cityId: cityId,
                        kind: kind,
                        category: category,
                        amount: amount,
                        title: title,
                        description: description,
                        referenceCode: referenceCode),
                    cancellationToken: cancellationToken)
                : Task.CompletedTask;
        }
    }
}
