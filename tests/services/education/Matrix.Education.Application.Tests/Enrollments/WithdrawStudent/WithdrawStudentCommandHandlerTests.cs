using System.Data;
using Matrix.Education.Application.Enrollments.WithdrawStudent;
using Matrix.Education.Application.Tests.TestSupport;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Application.Tests.Enrollments.WithdrawStudent
{
    public sealed class WithdrawStudentCommandHandlerTests
    {
        private static readonly SimulationHostId SimulationHostId =
            new(StudentProfileSynchronizationTestData.HostId);
        private static readonly ResidentId ResidentId = new(Guid.NewGuid());

        [Fact]
        public async Task Handle_ActiveEnrollment_ClosesEnrollmentAndReleasesSeat()
        {
            EducationInstitution institution = CreateInstitution();
            Assert.True(institution.TryReserveSeats(1));
            StudentEnrollment enrollment = CreateEnrollment(institution.EducationInstitutionId);
            var institutionRepository = new EducationInstitutionRepositoryStub(institution);
            var unitOfWork = new EducationUnitOfWorkStub();
            StudentProfile student = CreateStudent();
            var outboxWriter = new EducationStudentParticipationOutboxWriterStub();
            var handler = new WithdrawStudentCommandHandler(
                new StudentProfileRepositoryStub([student]),
                institutionRepository,
                new StudentEnrollmentRepositoryStub(enrollment),
                new EducationSimulationDeletionRepositoryStub(),
                outboxWriter,
                unitOfWork,
                new EducationFixedTimeProvider(StudentProfileSynchronizationTestData.SynchronizedAtUtc));

            WithdrawStudentResult result = await handler.Handle(
                CreateCommand(),
                CancellationToken.None);

            Assert.Equal(WithdrawStudentStatus.Applied, result.Status);
            Assert.Equal(enrollment.EnrollmentId.Value, result.EnrollmentId);
            Assert.Equal(EnrollmentStatus.Withdrawn, enrollment.Status);
            Assert.Equal(new DateOnly(2048, 5, 4), enrollment.ClosedOn);
            Assert.Equal(0, institution.CurrentEnrollmentCount);
            Assert.Equal(1, student.ParticipationRevision);
            Assert.False(Assert.Single(Assert.Single(outboxWriter.Batches).Students).IsEnrolled);
            Assert.Equal(1, unitOfWork.SaveCount);
            Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
        }

        [Fact]
        public async Task Handle_NoActiveEnrollment_ReturnsWithoutLoadingInstitution()
        {
            var institutionRepository = new EducationInstitutionRepositoryStub(CreateInstitution());
            var unitOfWork = new EducationUnitOfWorkStub();
            var handler = new WithdrawStudentCommandHandler(
                new StudentProfileRepositoryStub([CreateStudent()]),
                institutionRepository,
                new StudentEnrollmentRepositoryStub(),
                new EducationSimulationDeletionRepositoryStub(),
                new EducationStudentParticipationOutboxWriterStub(),
                unitOfWork,
                new EducationFixedTimeProvider(StudentProfileSynchronizationTestData.SynchronizedAtUtc));

            WithdrawStudentResult result = await handler.Handle(
                CreateCommand(),
                CancellationToken.None);

            Assert.Equal(WithdrawStudentStatus.NotEnrolled, result.Status);
            Assert.Equal(0, institutionRepository.GetCallCount);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_DeletedSimulation_DoesNotLoadEnrollment()
        {
            var enrollmentRepository = new StudentEnrollmentRepositoryStub(
                CreateEnrollment(EducationInstitutionId.New()));
            var handler = new WithdrawStudentCommandHandler(
                new StudentProfileRepositoryStub([CreateStudent()]),
                new EducationInstitutionRepositoryStub((EducationInstitution?)null),
                enrollmentRepository,
                new EducationSimulationDeletionRepositoryStub(
                    StudentProfileSynchronizationTestData.SynchronizedAtUtc),
                new EducationStudentParticipationOutboxWriterStub(),
                new EducationUnitOfWorkStub(),
                new EducationFixedTimeProvider(StudentProfileSynchronizationTestData.SynchronizedAtUtc));

            WithdrawStudentResult result = await handler.Handle(
                CreateCommand(),
                CancellationToken.None);

            Assert.Equal(WithdrawStudentStatus.SimulationDeleted, result.Status);
            Assert.Equal(0, enrollmentRepository.GetActiveCallCount);
        }

        private static WithdrawStudentCommand CreateCommand()
        {
            return new WithdrawStudentCommand(
                SimulationHostId: SimulationHostId.Value,
                ResidentId: ResidentId.Value,
                WithdrawnOn: new DateOnly(2048, 5, 4));
        }

        private static EducationInstitution CreateInstitution()
        {
            return EducationInstitution.Create(
                id: EducationInstitutionId.New(),
                simulationHostId: SimulationHostId,
                name: "Central school",
                kind: new EducationInstitutionKindKey("school"),
                capacity: 10);
        }

        private static StudentProfile CreateStudent()
        {
            return StudentProfile.Register(
                residentId: ResidentId,
                simulationHostId: SimulationHostId,
                birthDate: new DateOnly(2030, 5, 12),
                isAlive: true,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: StudentProfileSynchronizationTestData.SynchronizedAtUtc);
        }

        private static StudentEnrollment CreateEnrollment(EducationInstitutionId institutionId)
        {
            return StudentEnrollment.Enroll(
                id: EnrollmentId.New(),
                simulationHostId: SimulationHostId,
                residentId: ResidentId,
                institutionId: institutionId,
                stage: new EducationStageKey("upper-secondary"),
                enrolledOn: new DateOnly(2048, 5, 1));
        }
    }
}
