using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetExpense
{
    public sealed class RecordCityBudgetExpenseCommandHandler(
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository ledgerRepository,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<RecordCityBudgetExpenseCommand, BudgetLedgerEntryDto>
    {
        public async Task<BudgetLedgerEntryDto> Handle(
            RecordCityBudgetExpenseCommand request,
            CancellationToken cancellationToken)
        {
            CityBudget budget = await budgetRepository.GetByCityAsync(request.CityId, cancellationToken)
                ?? CreateBudget(request.CityId, budgetRepository);

            var entry = new CityBudgetLedgerEntry(
                id: Guid.NewGuid(),
                cityId: request.CityId,
                occurredAtUtc: DateTimeOffset.UtcNow,
                kind: CityBudgetLedgerEntryKind.Expense,
                category: request.Category,
                amount: Money.FromDecimal(request.Amount),
                title: request.Title,
                description: request.Description,
                source: CityBudgetLedgerEntrySource.Manual,
                referenceCode: null);

            budget.ApplyLedgerEntry(entry);
            await ledgerRepository.AddAsync(entry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Map(entry);
        }

        private static CityBudget CreateBudget(Guid cityId, ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(CityBudgetId.New(), cityId);
            budgetRepository.Add(budget);
            return budget;
        }

        private static BudgetLedgerEntryDto Map(CityBudgetLedgerEntry entry)
        {
            return new BudgetLedgerEntryDto(
                EntryId: entry.Id,
                OccurredAtUtc: entry.OccurredAtUtc.ToString("O"),
                Kind: entry.Kind.ToString(),
                Category: entry.Category.ToString(),
                Amount: entry.Amount.Amount,
                Title: entry.Title,
                Description: entry.Description,
                Source: entry.Source.ToString(),
                ReferenceCode: entry.ReferenceCode);
        }
    }
}
