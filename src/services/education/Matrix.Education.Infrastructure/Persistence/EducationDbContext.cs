using Matrix.BuildingBlocks.Infrastructure.Outbox.Models;
using Matrix.BuildingBlocks.Infrastructure.Outbox.Persistence;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Progression;
using Matrix.Education.Domain.Students;
using Matrix.Education.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Matrix.Education.Infrastructure.Persistence
{
    public sealed class EducationDbContext(DbContextOptions<EducationDbContext> options)
        : DbContext(options)
    {
        public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
        public DbSet<EducationSimulationRuntimeState> SimulationRuntimes => Set<EducationSimulationRuntimeState>();
        public DbSet<EducationInstitution> Institutions => Set<EducationInstitution>();
        public DbSet<StudentEnrollment> Enrollments => Set<StudentEnrollment>();
        public DbSet<EducationProgressionCheckpoint> ProgressionCheckpoints =>
            Set<EducationProgressionCheckpoint>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<EducationSimulationDeletionState> SimulationDeletionStates =>
            Set<EducationSimulationDeletionState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddOutboxMessageModel();
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(EducationDbContext).Assembly);
        }
    }
}
