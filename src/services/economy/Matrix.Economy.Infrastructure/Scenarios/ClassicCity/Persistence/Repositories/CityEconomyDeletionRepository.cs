using Matrix.Economy.Application.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityEconomyDeletionRepository(EconomyDbContext dbContext)
        : ICityEconomyDeletionRepository
    {
        public async Task<DateTimeOffset?> GetDeletedAtUtcAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            CityEconomyDeletionState? state = await dbContext.CityEconomyDeletionStates.FindAsync(
                keyValues: [cityId],
                cancellationToken: cancellationToken);

            return state?.DeletedAtUtc;
        }

        public async Task DeleteCityDataAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            if (dbContext.Database.IsRelational())
            {
                await DeleteRelationalDataAsync(
                    cityId: cityId,
                    cancellationToken: cancellationToken);
                return;
            }

            dbContext.CityHouseholdObligations.RemoveRange(dbContext.CityHouseholdObligations.Where(x => x.CityId == cityId));
            dbContext.CityBusinessLedgerEntries.RemoveRange(dbContext.CityBusinessLedgerEntries.Where(x => x.CityId == cityId));
            dbContext.CityHouseholdAccountLedgerEntries.RemoveRange(dbContext.CityHouseholdAccountLedgerEntries.Where(x => x.CityId == cityId));
            dbContext.CityBudgetSettlements.RemoveRange(dbContext.CityBudgetSettlements.Where(x => x.CityId == cityId));
            dbContext.CityBudgetAllocations.RemoveRange(dbContext.CityBudgetAllocations.Where(x => x.CityId == cityId));
            dbContext.CityBudgetLedgerEntries.RemoveRange(dbContext.CityBudgetLedgerEntries.Where(x => x.CityId == cityId));
            dbContext.CityEconomyCostProfileStates.RemoveRange(dbContext.CityEconomyCostProfileStates.Where(x => x.CityId == cityId));
            dbContext.CityEconomyProgressionStates.RemoveRange(dbContext.CityEconomyProgressionStates.Where(x => x.CityId == cityId));
            dbContext.CityHouseholdAccounts.RemoveRange(dbContext.CityHouseholdAccounts.Where(x => x.CityId == cityId));
            dbContext.CityBusinesses.RemoveRange(dbContext.CityBusinesses.Where(x => x.CityId == cityId));
            dbContext.CityBudgets.RemoveRange(dbContext.CityBudgets.Where(x => x.CityId == cityId));
        }

        public async Task RecordAsync(
            Guid cityId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc,
            CancellationToken cancellationToken)
        {
            CityEconomyDeletionState? state = await dbContext.CityEconomyDeletionStates.FindAsync(
                keyValues: [cityId],
                cancellationToken: cancellationToken);

            if (state is null)
            {
                await dbContext.CityEconomyDeletionStates.AddAsync(
                    entity: new CityEconomyDeletionState(
                        cityId: cityId,
                        deletedAtUtc: deletedAtUtc,
                        updatedAtUtc: updatedAtUtc),
                    cancellationToken: cancellationToken);
                return;
            }

            state.Record(
                deletedAtUtc: deletedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        private async Task DeleteRelationalDataAsync(
            Guid cityId,
            CancellationToken cancellationToken)
        {
            await dbContext.CityHouseholdObligations.Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
            await dbContext.CityBusinessLedgerEntries.Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
            await dbContext.CityHouseholdAccountLedgerEntries.Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
            await dbContext.CityBudgetSettlements.Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
            await dbContext.CityBudgetAllocations.Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
            await dbContext.CityBudgetLedgerEntries.Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
            await dbContext.CityEconomyCostProfileStates.Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
            await dbContext.CityEconomyProgressionStates.Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
            await dbContext.CityHouseholdAccounts.Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
            await dbContext.CityBusinesses.Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
            await dbContext.CityBudgets.Where(x => x.CityId == cityId)
               .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
