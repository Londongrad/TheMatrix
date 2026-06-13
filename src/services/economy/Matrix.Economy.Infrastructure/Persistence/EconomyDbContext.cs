using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence;
using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Matrix.Economy.Infrastructure.Scenarios.ClassicCity.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence
{
    public class EconomyDbContext(DbContextOptions<EconomyDbContext> options)
        : DbContext(options)
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
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddOutboxMessageModel();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EconomyDbContext).Assembly);
        }
    }
}
