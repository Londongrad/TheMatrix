using Matrix.Healthcare.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Persistence
{
    public sealed class HealthcareDbContext(DbContextOptions<HealthcareDbContext> options)
        : DbContext(options)
    {
        public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(HealthcareDbContext).Assembly);
        }
    }
}
