using System.Data;
using Matrix.Education.Application.Enrollments.CompleteStudentStage;
using Matrix.Education.Application.Tests.TestSupport;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Application.Tests.Enrollments.CompleteStudentStage
{
    public sealed class CompleteStudentStageCommandHandlerTests
    {
        private static readonly SimulationHostId SimulationHostId =
            new(StudentProfileSynchronizationTestData.HostId);
        private static readonly ResidentId ResidentId = new(Guid.NewGuid());

        [Fact]
        public async Task Handle_ActiveEnrollment_CompletesStageAndReleasesSeat()
        {
            StudentProfile student = CreateStudent();
            EducationInstitution institution = CreateInstitution();
            Assert.True(institution.TryReserveSeats(1));
            StudentEnrollment enrollment = CreateEnrollment(institution.EducationInstitutionId);
            var unitOfWork = new EducationUnitOfWorkStub();
            var outboxWriter = new EducationStudentParticipationOutboxWriterStub();
            var handler = new CompleteStudentStageCommandHandler(
                new StudentProfileRepositoryStub([student]),
                new EducationInstitutionRepositoryStub(institution),
                new StudentEnrollmentRepositoryStub(enrollment),
                new EducationSimulationDeletionRepositoryStub(),
                outboxWriter,
                unitOfWork,
                new EducationFixedTimeProvider(StudentProfileSynchronizationTestData.SynchronizedAtUtc));

            CompleteStudentStageResult result = await handler.Handle(
                CreateCommand(),
                CancellationToken.None);

            Assert.Equal(CompleteStudentStageStatus.Applied, result.Status);
            Assert.Equal(enrollment.EnrollmentId.Value, result.EnrollmentId);
            Assert.Equal("upper-secondary", result.CompletedStage);
            Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
            Assert.Equal(new EducationStageKey("upper-secondary"), student.CompletedStage);
            Assert.Equal(new DateOnly(2048, 6, 30), student.CompletedStageOn);
            Assert.Equal(0, institution.CurrentEnrollmentCount);
            Assert.Equal(1, student.ParticipationRevision);
            Assert.False(Assert.Single(Assert.Single(outboxWriter.Batches).Students).IsEnrolled);
            Assert.Equal(1, unitOfWork.SaveCount);
            Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
        }

        [Fact]
        public async Task Handle_UnavailableStudent_PreservesEnrollmentAndSeat()
        {
            StudentProfile student = CreateStudent();
            student.TryDeactivate(
                sourceRevision: 2,
                synchronizedAtUtc: StudentProfileSynchronizationTestData.SynchronizedAtUtc.AddMinutes(1));
            EducationInstitution institution = CreateInstitution();
            Assert.True(institution.TryReserveSeats(1));
            StudentEnrollment enrollment = CreateEnrollment(institution.EducationInstitutionId);
            var unitOfWork = new EducationUnitOfWorkStub();
            var handler = new CompleteStudentStageCommandHandler(
                new StudentProfileRepositoryStub([student]),
                new EducationInstitutionRepositoryStub(institution),
                new StudentEnrollmentRepositoryStub(enrollment),
                new EducationSimulationDeletionRepositoryStub(),
                new EducationStudentParticipationOutboxWriterStub(),
                unitOfWork,
                new EducationFixedTimeProvider(StudentProfileSynchronizationTestData.SynchronizedAtUtc));

            CompleteStudentStageResult result = await handler.Handle(
                CreateCommand(),
                CancellationToken.None);

            Assert.Equal(CompleteStudentStageStatus.StudentUnavailable, result.Status);
            Assert.Equal(EnrollmentStatus.Active, enrollment.Status);
            Assert.Equal(1, institution.CurrentEnrollmentCount);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_NoActiveEnrollment_DoesNotLoadStudent()
        {
            var studentRepository = new StudentProfileRepositoryStub([CreateStudent()]);
            var handler = new CompleteStudentStageCommandHandler(
                studentRepository,
                new EducationInstitutionRepositoryStub(CreateInstitution()),
                new StudentEnrollmentRepositoryStub(),
                new EducationSimulationDeletionRepositoryStub(),
                new EducationStudentParticipationOutboxWriterStub(),
                new EducationUnitOfWorkStub(),
                new EducationFixedTimeProvider(StudentProfileSynchronizationTestData.SynchronizedAtUtc));

            CompleteStudentStageResult result = await handler.Handle(
                CreateCommand(),
                CancellationToken.None);

            Assert.Equal(CompleteStudentStageStatus.NotEnrolled, result.Status);
            Assert.Equal(0, studentRepository.GetCallCount);
        }

        private static CompleteStudentStageCommand CreateCommand()
        {
            return new CompleteStudentStageCommand(
                SimulationHostId: SimulationHostId.Value,
                ResidentId: ResidentId.Value,
                CompletedOn: new DateOnly(2048, 6, 30));
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

        private static EducationInstitution CreateInstitution()
        {
            return EducationInstitution.Create(
                id: EducationInstitutionId.New(),
                simulationHostId: SimulationHostId,
                name: "Central school",
                kind: new EducationInstitutionKindKey("school"),
                capacity: 10);
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
