using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Persistence
{
    public sealed class HealthcareDbContext(DbContextOptions<HealthcareDbContext> options)
        : DbContext(options)
    {
        public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
        public DbSet<PatientMedicalRecord> PatientMedicalRecords => Set<PatientMedicalRecord>();
        public DbSet<HealthcareSimulationDeletionState> SimulationDeletionStates =>
            Set<HealthcareSimulationDeletionState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(HealthcareDbContext).Assembly);
        }
    }
}
