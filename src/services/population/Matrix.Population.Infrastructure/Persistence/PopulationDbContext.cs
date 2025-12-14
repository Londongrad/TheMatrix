using Matrix.Population.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Population.Infrastructure.Persistence
{
    public class PopulationDbContext(DbContextOptions<PopulationDbContext> options)
        : DbContext(options)
    {
        public DbSet<Person> Persons => Set<Person>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PopulationDbContext).Assembly);
        }
    }
}
