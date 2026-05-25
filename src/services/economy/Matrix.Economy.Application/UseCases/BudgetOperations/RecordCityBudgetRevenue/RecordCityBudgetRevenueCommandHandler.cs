using Matrix.BuildingBlocks.Domain.ValueObjects;
using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.BudgetLedger;
using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.ValueObjects;
using MediatR;

namespace Matrix.Economy.Application.UseCases.BudgetOperations.RecordCityBudgetRevenue
{
    public sealed class RecordCityBudgetRevenueCommandHandler(
        ICityBudgetRepository budgetRepository,
        ICityBudgetLedgerRepository ledgerRepository,
        IEconomyUnitOfWork unitOfWork,
        ICityOperationalBudgetSignalPublisher operationalBudgetSignalPublisher,
        ICityOperationalBudgetPressureProjectionService pressureProjectionService,
        TimeProvider timeProvider)
        : IRequestHandler<RecordCityBudgetRevenueCommand, BudgetLedgerEntryDto>
    {
        public async Task<BudgetLedgerEntryDto> Handle(
            RecordCityBudgetRevenueCommand request,
            CancellationToken cancellationToken)
        {
            BudgetLedgerEntryDto result = default!;

            await unitOfWork.ExecuteInTransactionAsync(
                action: async ct =>
                {
                    CityBudgetUnitProfile requestedUnit = ResolveRequestedUnit(request);

                    CityBudget budget = await budgetRepository.GetByCityAsync(
                                            cityId: request.CityId,
                                            cancellationToken: ct) ??
                                        CreateBudget(
                                            cityId: request.CityId,
                                            requestedUnit: requestedUnit,
                                            budgetRepository: budgetRepository);
                    budget.EnsureCompatibleUnit(requestedUnit);

                    DateTimeOffset occurredAtUtc = timeProvider.GetUtcNow();

                    var entry = new CityBudgetLedgerEntry(
                        id: Guid.NewGuid(),
                        cityId: request.CityId,
                        occurredAtUtc: occurredAtUtc,
                        kind: CityBudgetLedgerEntryKind.Revenue,
                        category: request.Category,
                        amount: Money.FromDecimal(request.Amount),
                        title: request.Title,
                        description: request.Description,
                        source: CityBudgetLedgerEntrySource.Manual,
                        referenceCode: null);

                    budget.ApplyLedgerEntry(entry);
                    await ledgerRepository.AddAsync(
                        entry: entry,
                        cancellationToken: ct);
                    await unitOfWork.SaveChangesAsync(ct);

                    CityOperationalBudgetPressureDto pressure = await pressureProjectionService.GetAsync(
                        cityId: request.CityId,
                        cancellationToken: ct);
                    await operationalBudgetSignalPublisher.PublishClassicCityOperationalBudgetPressureSnapshotAsync(
                        snapshot: pressure,
                        effectiveAtUtc: entry.OccurredAtUtc,
                        occurredAtUtc: timeProvider.GetUtcNow(),
                        cancellationToken: ct);
                    await unitOfWork.SaveChangesAsync(ct);

                    result = Map(
                        entry: entry,
                        unitProfile: budget.GetUnitProfile());
                },
                cancellationToken: cancellationToken);

            return result;
        }

        private static CityBudget CreateBudget(
            Guid cityId,
            CityBudgetUnitProfile requestedUnit,
            ICityBudgetRepository budgetRepository)
        {
            var budget = new CityBudget(
                id: CityBudgetId.New(),
                cityId: cityId,
                unitProfile: requestedUnit);
            budgetRepository.Add(budget);
            return budget;
        }

        private static CityBudgetUnitProfile ResolveRequestedUnit(RecordCityBudgetRevenueCommand request)
        {
            if (string.IsNullOrWhiteSpace(request.UnitCode) &&
                string.IsNullOrWhiteSpace(request.UnitDisplayName) &&
                string.IsNullOrWhiteSpace(request.UnitSymbol) &&
                string.IsNullOrWhiteSpace(request.UnitKind))
                return CityBudgetUnitProfile.DefaultMoney();

            if (!Enum.TryParse(
                    value: request.UnitKind,
                    ignoreCase: true,
                    result: out CityBudgetUnitKind unitKind))
                throw new InvalidOperationException($"Unsupported unit kind '{request.UnitKind}'.");

            return new CityBudgetUnitProfile(
                Kind: unitKind,
                Code: request.UnitCode ?? string.Empty,
                DisplayName: request.UnitDisplayName ?? string.Empty,
                Symbol: request.UnitSymbol ?? string.Empty);
        }

        private static BudgetLedgerEntryDto Map(
            CityBudgetLedgerEntry entry,
            CityBudgetUnitProfile unitProfile)
        {
            return new BudgetLedgerEntryDto(
                EntryId: entry.Id,
                OccurredAtUtc: entry.OccurredAtUtc.ToString("O"),
                UnitKind: unitProfile.Kind.ToString(),
                UnitCode: unitProfile.Code,
                UnitDisplayName: unitProfile.DisplayName,
                UnitSymbol: unitProfile.Symbol,
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
