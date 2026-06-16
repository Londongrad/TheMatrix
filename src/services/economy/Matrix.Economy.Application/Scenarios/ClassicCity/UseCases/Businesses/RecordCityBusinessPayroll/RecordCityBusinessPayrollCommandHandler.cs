using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.Businesses.RecordCityBusinessPayroll
{
    public sealed class RecordCityBusinessPayrollCommandHandler(
        ICityBusinessRepository businessRepository,
        ICityBusinessLedgerRepository businessLedgerRepository,
        ICityHouseholdAccountRepository householdAccountRepository,
        ICityHouseholdAccountLedgerRepository householdLedgerRepository,
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository budgetLedgerRepository,
        IEconomyUnitOfWork unitOfWork,
        TimeProvider timeProvider)
        : IRequestHandler<RecordCityBusinessPayrollCommand, CityBusinessLedgerEntryDto>
    {
        public async Task<CityBusinessLedgerEntryDto> Handle(
            RecordCityBusinessPayrollCommand request,
            CancellationToken cancellationToken)
        {
            CityBusiness business = await businessRepository.GetByIdAsync(
                                        businessId: request.BusinessId,
                                        cancellationToken: cancellationToken) ??
                                    throw new InvalidOperationException(
                                        $"Business '{request.BusinessId}' was not found.");
            CityHouseholdAccount householdAccount =
                await householdAccountRepository.GetByIdAsync(
                    householdAccountId: request.HouseholdAccountId,
                    cancellationToken: cancellationToken) ??
                throw new InvalidOperationException($"Household account '{request.HouseholdAccountId}' was not found.");

            business.EnsureCanIssuePayroll();

            if (business.CityId != householdAccount.CityId)
                throw new InvalidOperationException("Business and household account must belong to the same city.");

            householdAccount.EnsureCompatibleUnit(business.GetUnitProfile());

            var grossAmount = Money.FromDecimal(request.GrossAmount);
            var incomeTaxAmount = Money.FromDecimal(request.IncomeTaxAmount);

            if (incomeTaxAmount.IsNegative || incomeTaxAmount.Amount > grossAmount.Amount)
                throw new InvalidOperationException("Income tax amount must be between zero and gross payroll.");

            Money netAmount = grossAmount.Subtract(incomeTaxAmount);

            business.RecordOperatingExpense(grossAmount);
            householdAccount.ReceivePayroll(netAmount);

            CityBudget budget = await budgetRepository.GetByCityAsync(
                                    cityId: business.CityId,
                                    cancellationToken: cancellationToken) ??
                                CreateBudget(
                                    business: business,
                                    budgetRepository: budgetRepository);
            budget.EnsureCompatibleUnit(business.GetUnitProfile());

            DateTimeOffset occurredAtUtc = timeProvider.GetUtcNow();

            var businessEntry = new CityBusinessLedgerEntry(
                id: Guid.NewGuid(),
                businessId: business.Id,
                cityId: business.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityBusinessLedgerEntryKind.PayrollExpense,
                amount: grossAmount,
                taxAmount: incomeTaxAmount,
                title: request.Title,
                description: request.Description,
                source: CityBusinessLedgerEntrySource.Payroll,
                referenceCode: householdAccount.Id.ToString("N"));

            var householdEntry = new CityHouseholdAccountLedgerEntry(
                id: Guid.NewGuid(),
                householdAccountId: householdAccount.Id,
                cityId: householdAccount.CityId,
                occurredAtUtc: occurredAtUtc,
                kind: CityHouseholdAccountLedgerEntryKind.PayrollIncome,
                amount: netAmount,
                title: request.Title,
                description: request.Description,
                source: CityHouseholdAccountLedgerEntrySource.Payroll,
                referenceCode: business.Id.ToString("N"));

            await businessLedgerRepository.AddAsync(
                entry: businessEntry,
                cancellationToken: cancellationToken);
            await householdLedgerRepository.AddAsync(
                entry: householdEntry,
                cancellationToken: cancellationToken);

            if (incomeTaxAmount.IsPositive)
            {
                var budgetEntry = new CityBudgetLedgerEntry(
                    id: Guid.NewGuid(),
                    cityId: business.CityId,
                    occurredAtUtc: occurredAtUtc,
                    kind: CityBudgetLedgerEntryKind.Revenue,
                    category: CityBudgetCategory.Taxation,
                    amount: incomeTaxAmount,
                    title: $"{request.Title} withholding",
                    description: $"Payroll withholding from {business.Name}. {request.Description}".Trim(),
                    source: CityBudgetLedgerEntrySource.PayrollWithholding,
                    referenceCode: business.Id.ToString("N"));

                budget.ApplyLedgerEntry(budgetEntry);
                await budgetLedgerRepository.AddAsync(
                    entry: budgetEntry,
                    cancellationToken: cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Map(
                entry: businessEntry,
                unitProfile: business.GetUnitProfile());
        }

        private static CityBudget CreateBudget(
            CityBusiness business,
            ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(
                id: CityBudgetId.New(),
                cityId: business.CityId,
                unitProfile: business.GetUnitProfile());
            budgetRepository.Add(budget);
            return budget;
        }

        private static CityBusinessLedgerEntryDto Map(
            CityBusinessLedgerEntry entry,
            CityBudgetUnitProfile unitProfile)
        {
            return new CityBusinessLedgerEntryDto(
                EntryId: entry.Id,
                OccurredAtUtc: entry.OccurredAtUtc.ToString("O"),
                UnitKind: unitProfile.Kind.ToString(),
                UnitCode: unitProfile.Code,
                UnitDisplayName: unitProfile.DisplayName,
                UnitSymbol: unitProfile.Symbol,
                Kind: entry.Kind.ToString(),
                Amount: entry.Amount.Amount,
                TaxAmount: entry.TaxAmount.Amount,
                Title: entry.Title,
                Description: entry.Description,
                Source: entry.Source.ToString(),
                ReferenceCode: entry.ReferenceCode);
        }
    }
}
