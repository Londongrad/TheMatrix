using Matrix.Economy.Domain.Aggregates;
using Matrix.Economy.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Economy.Infrastructure.Persistence
{
    public class EconomyDbContext(DbContextOptions<EconomyDbContext> options) : DbContext(options)
    {
        public DbSet<CityBudget> CityBudgets => Set<CityBudget>();
        public DbSet<CityBudgetSettlement> CityBudgetSettlements => Set<CityBudgetSettlement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EconomyDbContext).Assembly);
        }
    }
}
