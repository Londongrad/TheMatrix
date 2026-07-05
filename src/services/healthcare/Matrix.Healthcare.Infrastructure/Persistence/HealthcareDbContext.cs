using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence;
using Matrix.Healthcare.Domain.Facilities;
using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Operations;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Healthcare.Infrastructure.Persistence
{
    public sealed class HealthcareDbContext(DbContextOptions<HealthcareDbContext> options)
        : DbContext(options)
    {
        public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
        public DbSet<PatientMedicalRecord> PatientMedicalRecords => Set<PatientMedicalRecord>();
        public DbSet<PatientCareNeed> PatientCareNeeds => Set<PatientCareNeed>();
        public DbSet<PatientCareAssignment> PatientCareAssignments =>
            Set<PatientCareAssignment>();
        public DbSet<PatientHealthProgressionBatchSet> PatientHealthProgressionBatchSets =>
            Set<PatientHealthProgressionBatchSet>();
        public DbSet<CareFacility> CareFacilities => Set<CareFacility>();
        public DbSet<CareServiceQualityState> CareServiceQualityStates =>
            Set<CareServiceQualityState>();
        public DbSet<CareMedicineSupplyState> CareMedicineSupplyStates =>
            Set<CareMedicineSupplyState>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<HealthcareSimulationDeletionState> SimulationDeletionStates =>
            Set<HealthcareSimulationDeletionState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddOutboxMessageModel();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(HealthcareDbContext).Assembly);
        }
    }
}
