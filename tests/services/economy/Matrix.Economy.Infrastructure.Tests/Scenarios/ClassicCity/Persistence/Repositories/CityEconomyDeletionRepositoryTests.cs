using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Enums;
using Matrix.Economy.Infrastructure.Persistence;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;
using static Matrix.Economy.Infrastructure.Tests.TestSupport.EconomyInfrastructureTestSupport;

namespace Matrix.Economy.Infrastructure.Tests.Scenarios.ClassicCity.Persistence.Repositories
{
    public sealed class CityEconomyDeletionRepositoryTests
    {
        [Fact]
        public async Task DeleteCityDataAsync_RemovesAllCityDataAndPreservesOtherCities()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var otherCityId = Guid.Parse("11111111-2222-3333-4444-555555555555");
            CityBusiness business = CreateBusiness(
                cityId,
                name: "Bakery",
                externalReferenceCode: "business-bakery",
                templateKey: "bakery");
            CityHouseholdAccount household = CreateHouseholdAccount(
                cityId,
                name: "Household 1",
                externalReferenceCode: "household-1");

            await using EconomyDbContext dbContext = CreateDbContext();
            dbContext.CityBudgets.AddRange(CreateBudget(cityId), CreateBudget(otherCityId));
            dbContext.CityBudgetAllocations.Add(
                CreateBudgetAllocation(cityId, CityBudgetCategory.General, targetAmount: 100m));
            dbContext.CityBudgetLedgerEntries.Add(CreateBudgetLedgerEntry(cityId));
            dbContext.CityBusinesses.Add(business);
            dbContext.CityBusinessLedgerEntries.Add(CreateBusinessLedgerEntry(business.Id, cityId));
            dbContext.CityHouseholdAccounts.Add(household);
            dbContext.CityHouseholdAccountLedgerEntries.Add(
                CreateHouseholdAccountLedgerEntry(household.Id, cityId));
            dbContext.CityHouseholdObligations.Add(
                CreateHouseholdObligation(
                    cityId,
                    householdAccountId: household.Id,
                    providerBusinessId: business.Id,
                    name: "Utilities"));
            dbContext.CityBudgetSettlements.Add(CreateBudgetSettlement(cityId, tickId: 3, correlationId: "tick-3"));
            dbContext.CityEconomyCostProfileStates.Add(CreateCostProfileState(cityId));
            dbContext.CityEconomyProgressionStates.Add(CreateProgressionState(cityId));
            await dbContext.SaveChangesAsync();

            var repository = new CityEconomyDeletionRepository(dbContext);
            DateTimeOffset deletedAtUtc = new(
                year: 2048,
                month: 6,
                day: 2,
                hour: 9,
                minute: 30,
                second: 0,
                offset: TimeSpan.Zero);

            await repository.DeleteCityDataAsync(cityId, CancellationToken.None);
            await repository.RecordAsync(
                cityId,
                deletedAtUtc,
                updatedAtUtc: deletedAtUtc.AddMinutes(1),
                CancellationToken.None);
            await dbContext.SaveChangesAsync();

            Assert.Empty(dbContext.CityBudgets.Where(x => x.CityId == cityId));
            Assert.Empty(dbContext.CityBudgetAllocations.Where(x => x.CityId == cityId));
            Assert.Empty(dbContext.CityBudgetLedgerEntries.Where(x => x.CityId == cityId));
            Assert.Empty(dbContext.CityBusinesses.Where(x => x.CityId == cityId));
            Assert.Empty(dbContext.CityBusinessLedgerEntries.Where(x => x.CityId == cityId));
            Assert.Empty(dbContext.CityHouseholdAccounts.Where(x => x.CityId == cityId));
            Assert.Empty(dbContext.CityHouseholdAccountLedgerEntries.Where(x => x.CityId == cityId));
            Assert.Empty(dbContext.CityHouseholdObligations.Where(x => x.CityId == cityId));
            Assert.Empty(dbContext.CityBudgetSettlements.Where(x => x.CityId == cityId));
            Assert.Empty(dbContext.CityEconomyCostProfileStates.Where(x => x.CityId == cityId));
            Assert.Empty(dbContext.CityEconomyProgressionStates.Where(x => x.CityId == cityId));
            Assert.Single(dbContext.CityBudgets.Where(x => x.CityId == otherCityId));
            Assert.Equal(deletedAtUtc, await repository.GetDeletedAtUtcAsync(cityId, CancellationToken.None));
        }

        [Fact]
        public async Task RecordAsync_NewerDeletion_ReplacesTombstoneTimestamp()
        {
            var cityId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            DateTimeOffset firstDeletion = new(
                year: 2048,
                month: 6,
                day: 2,
                hour: 9,
                minute: 30,
                second: 0,
                offset: TimeSpan.Zero);

            await using EconomyDbContext dbContext = CreateDbContext();
            var repository = new CityEconomyDeletionRepository(dbContext);

            await repository.RecordAsync(cityId, firstDeletion, firstDeletion, CancellationToken.None);
            await dbContext.SaveChangesAsync();
            await repository.RecordAsync(
                cityId,
                firstDeletion.AddMinutes(1),
                firstDeletion.AddMinutes(2),
                CancellationToken.None);
            await dbContext.SaveChangesAsync();

            Assert.Equal(
                firstDeletion.AddMinutes(1),
                await repository.GetDeletedAtUtcAsync(cityId, CancellationToken.None));
            Assert.Equal(1, await dbContext.CityEconomyDeletionStates.CountAsync());
        }
    }
}
