using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Domain.Models;
using Matrix.Economy.Domain.Services;
using Matrix.Economy.Domain.ValueObjects;
using Matrix.Economy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace Matrix.Economy.Infrastructure.Services
{
    internal sealed class CityEconomyBootstrapService(
        EconomyDbContext dbContext,
        ICityBudgetRepository budgetRepository,
        ICityBudgetAllocationRepository allocationRepository,
        ICityBudgetLedgerRepository budgetLedgerRepository,
        ICityBusinessRepository businessRepository,
        IEconomyUnitOfWork unitOfWork,
        CityEconomySimulationTemplatePolicy simulationTemplatePolicy)
        : ICityEconomyBootstrapService
    {
        private const string CityBudgetByCityConstraintName = "IX_City_Budget_city_id";

        private const string CityBudgetAllocationByCityCategoryConstraintName =
            "IX_City_Budget_Allocation_city_id_category";

        private const string CityBusinessByCityTemplateConstraintName = "IX_City_Business_city_id_template_key";

        public async Task<CityEconomyBootstrapResultDto> BootstrapAsync(
            Guid cityId,
            string simulationKind,
            string? economyProfile,
            DateTimeOffset createdAtUtc,
            CancellationToken cancellationToken = default)
        {
            CityEconomySimulationTemplate template = simulationTemplatePolicy.Resolve(
                simulationKind: simulationKind,
                economyProfile: economyProfile);

            (CityBudget budget, bool budgetCreated) = await EnsureBudgetAsync(
                cityId: cityId,
                createdAtUtc: createdAtUtc,
                template: template,
                cancellationToken: cancellationToken);

            int createdAllocations = 0;
            foreach (CityEconomyAllocationTemplate allocationTemplate in template.DefaultAllocations)
            {
                bool created = await EnsureAllocationAsync(
                    cityId: cityId,
                    createdAtUtc: createdAtUtc,
                    template: template,
                    allocationTemplate: allocationTemplate,
                    cancellationToken: cancellationToken);

                if (created)
                    createdAllocations++;
            }

            int createdBusinesses = 0;
            foreach (CityEconomyBusinessTemplate businessTemplate in template.DefaultBusinesses)
            {
                bool created = await EnsureBusinessAsync(
                    cityId: cityId,
                    createdAtUtc: createdAtUtc,
                    template: template,
                    businessTemplate: businessTemplate,
                    cancellationToken: cancellationToken);

                if (created)
                    createdBusinesses++;
            }

            return new CityEconomyBootstrapResultDto(
                CityId: cityId,
                BudgetCreated: budgetCreated,
                CreatedAllocations: createdAllocations,
                CreatedBusinesses: createdBusinesses,
                UnitKind: budget.UnitKind.ToString(),
                UnitCode: budget.UnitCode,
                UnitDisplayName: budget.UnitDisplayName,
                UnitSymbol: budget.UnitSymbol);
        }

        private async Task<(CityBudget Budget, bool Created)> EnsureBudgetAsync(
            Guid cityId,
            DateTimeOffset createdAtUtc,
            CityEconomySimulationTemplate template,
            CancellationToken cancellationToken)
        {
            CityBudget? existingBudget = await budgetRepository.GetByCityAsync(
                cityId: cityId,
                cancellationToken: cancellationToken);

            if (existingBudget is not null)
            {
                existingBudget.EnsureCompatibleUnit(template.UnitProfile);
                return (existingBudget, false);
            }

            var budget = new CityBudget(
                id: CityBudgetId.New(),
                cityId: cityId,
                unitProfile: template.UnitProfile);
            budgetRepository.Add(budget);
            await ApplyInitialReserveAsync(
                budget: budget,
                cityId: cityId,
                createdAtUtc: createdAtUtc,
                template: template,
                cancellationToken: cancellationToken);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return (budget, true);
            }
            catch (DbUpdateException ex) when (IsConstraintViolation(
                                                   exception: ex,
                                                   constraintName: CityBudgetByCityConstraintName))
            {
                DetachAddedEntities();
                CityBudget? concurrentBudget = await budgetRepository.GetByCityAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);

                if (concurrentBudget is not null)
                {
                    concurrentBudget.EnsureCompatibleUnit(template.UnitProfile);
                    return (concurrentBudget, false);
                }

                throw;
            }
        }

        private async Task<bool> EnsureAllocationAsync(
            Guid cityId,
            DateTimeOffset createdAtUtc,
            CityEconomySimulationTemplate template,
            CityEconomyAllocationTemplate allocationTemplate,
            CancellationToken cancellationToken)
        {
            CityBudgetAllocation? existing = await allocationRepository.GetByCityAndCategoryAsync(
                cityId: cityId,
                category: allocationTemplate.Category,
                cancellationToken: cancellationToken);

            if (existing is not null)
            {
                existing.EnsureCompatibleUnit(template.UnitProfile);
                return false;
            }

            allocationRepository.Add(
                new CityBudgetAllocation(
                    id: Guid.NewGuid(),
                    cityId: cityId,
                    category: allocationTemplate.Category,
                    createdAtUtc: createdAtUtc,
                    unitProfile: template.UnitProfile,
                    targetAmount: allocationTemplate.TargetAmount));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException ex) when (IsConstraintViolation(
                                                   exception: ex,
                                                   constraintName: CityBudgetAllocationByCityCategoryConstraintName))
            {
                DetachAddedEntities();
                return false;
            }
        }

        private async Task<bool> EnsureBusinessAsync(
            Guid cityId,
            DateTimeOffset createdAtUtc,
            CityEconomySimulationTemplate template,
            CityEconomyBusinessTemplate businessTemplate,
            CancellationToken cancellationToken)
        {
            CityBusiness? existing = await businessRepository.GetByCityAndTemplateKeyAsync(
                cityId: cityId,
                templateKey: businessTemplate.TemplateKey,
                cancellationToken: cancellationToken);

            if (existing is not null)
            {
                existing.EnsureCompatibleUnit(template.UnitProfile);
                return false;
            }

            businessRepository.Add(
                new CityBusiness(
                    id: Guid.NewGuid(),
                    cityId: cityId,
                    name: businessTemplate.Name,
                    externalReferenceCode: null,
                    templateKey: businessTemplate.TemplateKey,
                    kind: businessTemplate.Kind,
                    createdAtUtc: createdAtUtc,
                    unitProfile: template.UnitProfile,
                    initialCapital: businessTemplate.StartingCapital));

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateException ex) when (IsConstraintViolation(
                                                   exception: ex,
                                                   constraintName: CityBusinessByCityTemplateConstraintName))
            {
                DetachAddedEntities();
                return false;
            }
        }

        private async Task ApplyInitialReserveAsync(
            CityBudget budget,
            Guid cityId,
            DateTimeOffset createdAtUtc,
            CityEconomySimulationTemplate template,
            CancellationToken cancellationToken)
        {
            if (template.InitialReserve.Amount <= 0m)
                return;

            var ledgerEntry = new CityBudgetLedgerEntry(
                id: Guid.NewGuid(),
                cityId: cityId,
                occurredAtUtc: createdAtUtc,
                kind: CityBudgetLedgerEntryKind.Revenue,
                category: CityBudgetCategory.General,
                amount: template.InitialReserve,
                title: "Initial treasury reserve",
                description: "Seeded from the city economy profile during simulation initialization.",
                source: CityBudgetLedgerEntrySource.Initialization,
                referenceCode: $"city-init-reserve:{cityId}");

            budget.ApplyLedgerEntry(ledgerEntry);
            await budgetLedgerRepository.AddAsync(
                entry: ledgerEntry,
                cancellationToken: cancellationToken);
        }

        private void DetachAddedEntities()
        {
            foreach (EntityEntry entry in dbContext.ChangeTracker.Entries()
                        .Where(x => x.State == EntityState.Added))
                entry.State = EntityState.Detached;
        }

        private static bool IsConstraintViolation(
            DbUpdateException exception,
            string constraintName)
        {
            return exception.InnerException is PostgresException
                   {
                       SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: var actualConstraintName
                   } &&
                   string.Equals(
                       a: actualConstraintName,
                       b: constraintName,
                       comparisonType: StringComparison.Ordinal);
        }
    }
}
