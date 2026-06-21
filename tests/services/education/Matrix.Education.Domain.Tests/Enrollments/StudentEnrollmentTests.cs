using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Domain.Tests.Enrollments
{
    public sealed class StudentEnrollmentTests
    {
        [Fact]
        public void Enroll_CreatesActiveEnrollment()
        {
            StudentEnrollment enrollment = CreateEnrollment();

            Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
            Assert.True(enrollment.IsActive);
            Assert.Null(enrollment.ClosedOn);
        }

        [Fact]
        public void Complete_ClosesEnrollment()
        {
            StudentEnrollment enrollment = CreateEnrollment();
            DateOnly completedOn = new(2026, 6, 1);

            bool changed = enrollment.Complete(completedOn);

            Assert.True(changed);
            Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
            Assert.Equal(completedOn, enrollment.ClosedOn);
        }

        [Fact]
        public void Complete_WhenAlreadyCompleted_IsIdempotent()
        {
            StudentEnrollment enrollment = CreateEnrollment();
            DateOnly completedOn = new(2026, 6, 1);
            enrollment.Complete(completedOn);

            bool changed = enrollment.Complete(completedOn.AddDays(1));

            Assert.False(changed);
            Assert.Equal(completedOn, enrollment.ClosedOn);
        }

        [Fact]
        public void Withdraw_ClosesEnrollment()
        {
            StudentEnrollment enrollment = CreateEnrollment();
            DateOnly withdrawnOn = new(2026, 4, 12);

            bool changed = enrollment.Withdraw(withdrawnOn);

            Assert.True(changed);
            Assert.Equal(EnrollmentStatus.Withdrawn, enrollment.Status);
            Assert.Equal(withdrawnOn, enrollment.ClosedOn);
        }

        [Fact]
        public void Withdraw_AfterCompletion_IsRejected()
        {
            StudentEnrollment enrollment = CreateEnrollment();
            enrollment.Complete(new DateOnly(2026, 6, 1));

            Assert.Throws<InvalidOperationException>(
                () => enrollment.Withdraw(new DateOnly(2026, 6, 2)));
        }

        [Fact]
        public void Complete_BeforeEnrollmentDate_IsRejected()
        {
            StudentEnrollment enrollment = CreateEnrollment();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => enrollment.Complete(new DateOnly(2025, 8, 31)));
        }

        private static StudentEnrollment CreateEnrollment()
        {
            return StudentEnrollment.Enroll(
                id: EnrollmentId.New(),
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                residentId: new ResidentId(Guid.NewGuid()),
                institutionId: EducationInstitutionId.New(),
                stage: new EducationStageKey("higher-education"),
                enrolledOn: new DateOnly(2025, 9, 1));
        }
    }
}
