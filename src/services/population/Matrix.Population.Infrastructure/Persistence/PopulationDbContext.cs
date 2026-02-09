using Matrix.Population.Domain.Entities;
using Matrix.Population.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence
{
    public class PopulationDbContext(DbContextOptions<PopulationDbContext> options)
        : DbContext(options)
    {
        public DbSet<CityPopulationArchiveState> CityPopulationArchiveStates => Set<CityPopulationArchiveState>();
        public DbSet<CityPopulationDeletionState> CityPopulationDeletionStates => Set<CityPopulationDeletionState>();
        public DbSet<CityPopulationEnvironment> CityPopulationEnvironments => Set<CityPopulationEnvironment>();

        public DbSet<CityPopulationProgressionState> CityPopulationProgressionStates
            => Set<CityPopulationProgressionState>();

        public DbSet<CityPopulationWeatherExposureState> CityPopulationWeatherExposureStates
            => Set<CityPopulationWeatherExposureState>();

        public DbSet<CityPopulationWeatherImpactState> CityPopulationWeatherImpactStates
            => Set<CityPopulationWeatherImpactState>();

        public DbSet<Household> Households => Set<Household>();
        public DbSet<Person> Persons => Set<Person>();
        public DbSet<ProcessedIntegrationMessage> ProcessedIntegrationMessages => Set<ProcessedIntegrationMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PopulationDbContext).Assembly);
        }
    }
}
