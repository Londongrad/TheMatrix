using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Students;
using Matrix.Education.Infrastructure.Persistence;
using Matrix.Education.Infrastructure.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Matrix.Education.Infrastructure.Tests.Persistence
{
    public sealed class EducationPersistenceModelTests
    {
        [Fact]
        public void StudentProfiles_HaveTickCandidateIndex()
        {
            using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            IEntityType entityType = dbContext.Model.FindEntityType(typeof(StudentProfile))!;

            IIndex index = FindIndex(
                entityType,
                "ix_education_profiles_tick_candidates");

            Assert.Equal(
                new[]
                {
                    nameof(StudentProfile.SimulationHostId),
                    nameof(StudentProfile.IsActive),
                    nameof(StudentProfile.IsAlive),
                    nameof(StudentProfile.BirthDate)
                },
                index.Properties.Select(property => property.Name));
        }

        [Fact]
        public void Enrollments_HaveBulkProgressionIndexes()
        {
            using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            IEntityType entityType = dbContext.Model.FindEntityType(typeof(StudentEnrollment))!;

            IIndex tickCandidates = FindIndex(
                entityType,
                "ix_education_enrollments_tick_candidates");
            IIndex institutionStatus = FindIndex(
                entityType,
                "ix_education_enrollments_institution_status");

            Assert.Equal(
                new[]
                {
                    nameof(StudentEnrollment.SimulationHostId),
                    nameof(StudentEnrollment.Status),
                    nameof(StudentEnrollment.Stage),
                    nameof(StudentEnrollment.ResidentId)
                },
                tickCandidates.Properties.Select(property => property.Name));
            Assert.Equal(
                new[]
                {
                    nameof(StudentEnrollment.InstitutionId),
                    nameof(StudentEnrollment.Status)
                },
                institutionStatus.Properties.Select(property => property.Name));
        }

        [Fact]
        public void Enrollments_AllowOnlyOneActiveEnrollmentPerResident()
        {
            using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            IEntityType entityType = dbContext.Model.FindEntityType(typeof(StudentEnrollment))!;

            IIndex index = FindIndex(
                entityType,
                "ux_education_enrollments_active_resident");

            Assert.True(index.IsUnique);
            Assert.Equal("status = 'Active'", index.GetFilter());
            Assert.Equal(
                new[]
                {
                    nameof(StudentEnrollment.SimulationHostId),
                    nameof(StudentEnrollment.ResidentId)
                },
                index.Properties.Select(property => property.Name));
        }

        [Fact]
        public void Institutions_UseOptimisticConcurrencyForCapacityUpdates()
        {
            using EducationDbContext dbContext =
                EducationInfrastructureTestSupport.CreateDbContext();
            IEntityType entityType = dbContext.Model.FindEntityType(typeof(EducationInstitution))!;
            IProperty versionProperty = entityType.FindProperty("xmin")!;

            Assert.True(versionProperty.IsConcurrencyToken);
            Assert.Equal(ValueGenerated.OnAddOrUpdate, versionProperty.ValueGenerated);
        }

        private static IIndex FindIndex(IEntityType entityType, string databaseName)
        {
            return entityType.GetIndexes()
               .Single(index => index.GetDatabaseName() == databaseName);
        }
    }
}
