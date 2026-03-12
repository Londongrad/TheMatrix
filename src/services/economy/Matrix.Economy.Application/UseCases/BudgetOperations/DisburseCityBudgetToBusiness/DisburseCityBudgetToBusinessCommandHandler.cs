using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetOperations.DisburseCityBudgetToBusiness
{
    public sealed class DisburseCityBudgetToBusinessCommandHandler(
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository budgetLedgerRepository,
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        IEconomyUnitOfWork unitOfWork)
        : IRequestHandler<DisburseCityBudgetToBusinessCommand, BudgetLedgerEntryDto>
    {
        public async Task<BudgetLedgerEntryDto> Handle(
            DisburseCityBudgetToBusinessCommand request,
            CancellationToken cancellationToken)
        {
            CityBusiness business = await businessRepository.GetByIdAsync(request.BusinessId, cancellationToken)
                ?? throw new InvalidOperationException($"Business '{request.BusinessId}' was not found.");

            if (business.CityId != request.CityId)
            {
                throw new InvalidOperationException("Business and budget must belong to the same city.");
            }

            CityBudget budget = await budgetRepository.GetByCityAsync(request.CityId, cancellationToken)
                ?? CreateBudget(request.CityId, business.GetUnitProfile(), budgetRepository);
            budget.EnsureCompatibleUnit(business.GetUnitProfile());

            Money amount = Money.FromDecimal(request.Amount);
            business.RecordMunicipalRevenue(amount);

            DateTimeOffset occurredAtUtc = DateTimeOffset.UtcNow;

            var budgetEntry = new CityBudgetLedgerEntry(
                id: Guid.NewGuid(),
                cityId: request.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityBudgetLedgerEntryKind.Expense,
                category: request.Category,
                amount: amount,
                title: request.Title,
                description: request.Description,
                source: CityBudgetLedgerEntrySource.MunicipalDisbursement,
                referenceCode: business.Id.ToString("N"));

            var businessEntry = new CityBusinessLedgerEntry(
                id: Guid.NewGuid(),
                businessId: business.Id,
                cityId: business.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityBusinessLedgerEntryKind.MunicipalRevenue,
                amount: amount,
                taxAmount: Money.Zero,
                title: request.Title,
                description: request.Description,
                source: CityBusinessLedgerEntrySource.MunicipalBudget,
                referenceCode: budget.CityId.ToString("N"));

            budget.ApplyLedgerEntry(budgetEntry);
            await budgetLedgerRepository.AddAsync(budgetEntry, cancellationToken);
            await businessLedgerRepository.AddAsync(businessEntry, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new BudgetLedgerEntryDto(
                EntryId: budgetEntry.Id,
                OccurredAtUtc: budgetEntry.OccurredAtUtc.ToString("O"),
                UnitKind: budget.UnitKind.ToString(),
                UnitCode: budget.UnitCode,
                UnitDisplayName: budget.UnitDisplayName,
                UnitSymbol: budget.UnitSymbol,
                Kind: budgetEntry.Kind.ToString(),
                Category: budgetEntry.Category.ToString(),
                Amount: budgetEntry.Amount.Amount,
                Title: budgetEntry.Title,
                Description: budgetEntry.Description,
                Source: budgetEntry.Source.ToString(),
                ReferenceCode: budgetEntry.ReferenceCode);
        }

        private static CityBudget CreateBudget(
            Guid cityId,
            CityBudgetUnitProfile unitProfile,
            ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(CityBudgetId.New(), cityId, unitProfile);
            budgetRepository.Add(budget);
            return budget;
        }
    }
}
