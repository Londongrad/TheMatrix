using MassTransit;
using Matrix.BuildingBlocks.Application.IntegrationEvents.Economy;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Application.UseCases.BudgetOperations.Common;
using Matrix.Economy.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Consumers
{
    public sealed class ClassicCityOperationalExpenseConsumer(
        CityBudgetOperationalExpenseSupport operationalExpenseSupport,
        IEconomyUnitOfWork unitOfWork,
        ILogger<ClassicCityOperationalExpenseConsumer> logger)
        : IConsumer<ClassicCityOperationalExpenseIncurredV1>
    {
        public async Task Consume(ConsumeContext<ClassicCityOperationalExpenseIncurredV1> context)
        {
            ClassicCityOperationalExpenseIncurredV1 message = context.Message;

            if (!Enum.TryParse(
                    value: message.Category,
                    ignoreCase: true,
                    result: out CityBudgetCategory category))
            {
                logger.LogWarning(
                    message:
                    "Skipped classic city operational expense for cityId={CityId}, expenseId={ExpenseId}; unsupported budget category '{Category}'.",
                    message.CityId,
                    message.ExpenseId,
                    message.Category);
                return;
            }

            string referenceCode = $"{message.SourceService}:{message.OperationKind}:{message.ExpenseId:N}";
            BudgetLedgerEntryDto? entry = await operationalExpenseSupport.RecordAsync(
                cityId: message.CityId,
                category: category,
                amount: message.Amount,
                title: message.Title,
                description: message.Description,
                occurredAtUtc: message.OccurredAtUtc,
                referenceCode: referenceCode,
                cancellationToken: context.CancellationToken);

            if (entry is null)
            {
                logger.LogDebug(
                    message:
                    "Skipped duplicate classic city operational expense for cityId={CityId}, expenseId={ExpenseId}, sourceService={SourceService}, operationKind={OperationKind}.",
                    message.CityId,
                    message.ExpenseId,
                    message.SourceService,
                    message.OperationKind);
                return;
            }

            BudgetLedgerEntryDto recordedEntry = entry;
            await unitOfWork.SaveChangesAsync(context.CancellationToken);

            logger.LogInformation(
                message:
                "Recorded classic city operational expense for cityId={CityId}, expenseId={ExpenseId}, category={Category}, amount={Amount}, sourceService={SourceService}, operationKind={OperationKind}.",
                message.CityId,
                message.ExpenseId,
                recordedEntry.Category,
                recordedEntry.Amount,
                message.SourceService,
                message.OperationKind);
        }
    }
}
