using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Scenarios.ClassicCity.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence
{
    public partial class EconomyDbContext
    {
        public DbSet<CityBusiness> CityBusinesses => Set<CityBusiness>();
        public DbSet<CityBusinessLedgerEntry> CityBusinessLedgerEntries => Set<CityBusinessLedgerEntry>();
        public DbSet<CityHouseholdAccount> CityHouseholdAccounts => Set<CityHouseholdAccount>();

        public DbSet<CityHouseholdAccountLedgerEntry> CityHouseholdAccountLedgerEntries
            => Set<CityHouseholdAccountLedgerEntry>();

        public DbSet<CityHouseholdObligation> CityHouseholdObligations => Set<CityHouseholdObligation>();
        public DbSet<CityBudget> CityBudgets => Set<CityBudget>();
        public DbSet<CityBudgetAllocation> CityBudgetAllocations => Set<CityBudgetAllocation>();
        public DbSet<CityBudgetLedgerEntry> CityBudgetLedgerEntries => Set<CityBudgetLedgerEntry>();
        public DbSet<CityEconomyCostProfileState> CityEconomyCostProfileStates => Set<CityEconomyCostProfileState>();
        public DbSet<CityEconomyProgressionState> CityEconomyProgressionStates => Set<CityEconomyProgressionState>();
        public DbSet<CityBudgetSettlement> CityBudgetSettlements => Set<CityBudgetSettlement>();
        public DbSet<CityEconomyDeletionState> CityEconomyDeletionStates => Set<CityEconomyDeletionState>();
    }
}
