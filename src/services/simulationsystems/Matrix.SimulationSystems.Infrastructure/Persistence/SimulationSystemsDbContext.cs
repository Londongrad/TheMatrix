using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems;
using Microsoft.EntityFrameworkCore;

namespace Matrix.SimulationSystems.Infrastructure.Persistence
{
    public class SimulationSystemsDbContext(DbContextOptions<SimulationSystemsDbContext> options)
        : DbContext(options)
    {
        public DbSet<CityEnvironmentalConditionState> CityEnvironmentalConditions => Set<CityEnvironmentalConditionState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SimulationSystemsDbContext).Assembly);
        }
    }
}
