using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence
{
    public class EconomyDbContext(DbContextOptions<EconomyDbContext> options) : DbContext(options)
    {
        public DbSet<CityBusiness> CityBusinesses => Set<CityBusiness>();
        public DbSet<CityBusinessLedgerEntry> CityBusinessLedgerEntries => Set<CityBusinessLedgerEntry>();
        public DbSet<CityHouseholdAccount> CityHouseholdAccounts => Set<CityHouseholdAccount>();
        public DbSet<CityHouseholdAccountLedgerEntry> CityHouseholdAccountLedgerEntries => Set<CityHouseholdAccountLedgerEntry>();
        public DbSet<CityHouseholdObligation> CityHouseholdObligations => Set<CityHouseholdObligation>();
        public DbSet<CityBudget> CityBudgets => Set<CityBudget>();
        public DbSet<CityBudgetAllocation> CityBudgetAllocations => Set<CityBudgetAllocation>();
        public DbSet<CityBudgetLedgerEntry> CityBudgetLedgerEntries => Set<CityBudgetLedgerEntry>();
        public DbSet<CityBudgetSettlement> CityBudgetSettlements => Set<CityBudgetSettlement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EconomyDbContext).Assembly);
        }
    }
}
