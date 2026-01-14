using Matrix.CityCore.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Matrix.CityCore.Infrastructure.Persistence
{
    public class CityCoreDbContext(DbContextOptions<CityCoreDbContext> options)
        : DbContext(options)
    {
        public DbSet<CityClock> CityClocks => Set<CityClock>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CityCoreDbContext).Assembly);
        }
    }
}
