using System.Data;
using Matrix.Education.Application.Enrollments.EnrollStudent;
using Matrix.Education.Application.Tests.TestSupport;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Application.Tests.Enrollments.EnrollStudent
{
    public sealed class EnrollStudentCommandHandlerTests
    {
        private static readonly Guid ResidentId =
            Guid.Parse("22222222-2222-2222-2222-222222222222");

        [Fact]
        public async Task Handle_AvailableStudentAndInstitution_ReservesSeatAndCreatesEnrollment()
        {
            StudentProfile profile = CreateProfile();
            EducationInstitution institution = CreateInstitution(capacity: 2);
            var institutionRepository = new EducationInstitutionRepositoryStub(institution);
            var enrollmentRepository = new StudentEnrollmentRepositoryStub();
            var unitOfWork = new EducationUnitOfWorkStub();
            var outboxWriter = new EducationStudentParticipationOutboxWriterStub();
            EnrollStudentCommandHandler handler = CreateHandler(
                profile,
                institutionRepository,
                enrollmentRepository,
                unitOfWork: unitOfWork,
                outboxWriter: outboxWriter);

            EnrollStudentResult result = await handler.Handle(
                CreateCommand(institution.EducationInstitutionId.Value),
                CancellationToken.None);

            Assert.Equal(EnrollStudentStatus.Applied, result.Status);
            Assert.NotNull(result.EnrollmentId);
            StudentEnrollment added = Assert.Single(enrollmentRepository.Added);
            Assert.Equal(new ResidentId(ResidentId), added.ResidentId);
            Assert.Equal(new EducationStageKey("upper-secondary"), added.Stage);
            Assert.Equal(1, institution.CurrentEnrollmentCount);
            Assert.Equal(1, profile.ParticipationRevision);
            Assert.True(Assert.Single(Assert.Single(outboxWriter.Batches).Students).IsEnrolled);
            Assert.Equal(1, unitOfWork.SaveCount);
            Assert.Equal(IsolationLevel.Serializable, unitOfWork.LastIsolationLevel);
        }

        [Fact]
        public async Task Handle_ExistingEnrollment_ReturnsIdentifierWithoutReservingAnotherSeat()
        {
            StudentProfile profile = CreateProfile();
            EducationInstitution institution = CreateInstitution(capacity: 2);
            StudentEnrollment existing = StudentEnrollment.Enroll(
                id: EnrollmentId.New(),
                simulationHostId: new SimulationHostId(StudentProfileSynchronizationTestData.HostId),
                residentId: new ResidentId(ResidentId),
                institutionId: institution.EducationInstitutionId,
                stage: new EducationStageKey("upper-secondary"),
                enrolledOn: new DateOnly(2048, 5, 1));
            var enrollmentRepository = new StudentEnrollmentRepositoryStub(existing);
            var unitOfWork = new EducationUnitOfWorkStub();
            EnrollStudentCommandHandler handler = CreateHandler(
                profile,
                new EducationInstitutionRepositoryStub(institution),
                enrollmentRepository,
                unitOfWork: unitOfWork);

            EnrollStudentResult result = await handler.Handle(
                CreateCommand(institution.EducationInstitutionId.Value),
                CancellationToken.None);

            Assert.Equal(EnrollStudentStatus.AlreadyEnrolled, result.Status);
            Assert.Equal(existing.EnrollmentId.Value, result.EnrollmentId);
            Assert.Equal(0, institution.CurrentEnrollmentCount);
            Assert.Empty(enrollmentRepository.Added);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        [Fact]
        public async Task Handle_InactiveStudent_DoesNotLoadInstitution()
        {
            StudentProfile profile = StudentProfile.Register(
                residentId: new ResidentId(ResidentId),
                simulationHostId: new SimulationHostId(StudentProfileSynchronizationTestData.HostId),
                birthDate: new DateOnly(2030, 5, 12),
                isAlive: true,
                isActive: false,
                sourceRevision: 1,
                synchronizedAtUtc: StudentProfileSynchronizationTestData.SynchronizedAtUtc);
            var institutionRepository = new EducationInstitutionRepositoryStub(CreateInstitution(2));
            EnrollStudentCommandHandler handler = CreateHandler(
                profile,
                institutionRepository,
                new StudentEnrollmentRepositoryStub());

            EnrollStudentResult result = await handler.Handle(
                CreateCommand(Guid.NewGuid()),
                CancellationToken.None);

            Assert.Equal(EnrollStudentStatus.StudentUnavailable, result.Status);
            Assert.Equal(0, institutionRepository.GetCallCount);
        }

        [Fact]
        public async Task Handle_StudentFromAnotherSimulation_ReturnsNotFound()
        {
            StudentProfile foreignProfile = StudentProfileSynchronizationTestData.CreateProfile(
                residentId: ResidentId,
                sourceRevision: 1,
                simulationHostId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
            var institutionRepository = new EducationInstitutionRepositoryStub(CreateInstitution(2));
            EnrollStudentCommandHandler handler = CreateHandler(
                foreignProfile,
                institutionRepository,
                new StudentEnrollmentRepositoryStub());

            EnrollStudentResult result = await handler.Handle(
                CreateCommand(Guid.NewGuid()),
                CancellationToken.None);

            Assert.Equal(EnrollStudentStatus.StudentNotFound, result.Status);
            Assert.Equal(0, institutionRepository.GetCallCount);
        }

        [Fact]
        public async Task Handle_FullInstitution_DoesNotCreateEnrollment()
        {
            EducationInstitution institution = CreateInstitution(capacity: 1);
            Assert.True(institution.TryReserveSeats(1));
            var enrollmentRepository = new StudentEnrollmentRepositoryStub();
            var unitOfWork = new EducationUnitOfWorkStub();
            EnrollStudentCommandHandler handler = CreateHandler(
                CreateProfile(),
                new EducationInstitutionRepositoryStub(institution),
                enrollmentRepository,
                unitOfWork: unitOfWork);

            EnrollStudentResult result = await handler.Handle(
                CreateCommand(institution.EducationInstitutionId.Value),
                CancellationToken.None);

            Assert.Equal(EnrollStudentStatus.CapacityUnavailable, result.Status);
            Assert.Empty(enrollmentRepository.Added);
            Assert.Equal(0, unitOfWork.SaveCount);
        }

        private static EnrollStudentCommandHandler CreateHandler(
            StudentProfile profile,
            EducationInstitutionRepositoryStub institutionRepository,
            StudentEnrollmentRepositoryStub enrollmentRepository,
            EducationUnitOfWorkStub? unitOfWork = null,
            EducationStudentParticipationOutboxWriterStub? outboxWriter = null)
        {
            return new EnrollStudentCommandHandler(
                studentProfileRepository: new StudentProfileRepositoryStub([profile]),
                institutionRepository: institutionRepository,
                enrollmentRepository: enrollmentRepository,
                deletionRepository: new EducationSimulationDeletionRepositoryStub(),
                participationOutboxWriter:
                    outboxWriter ?? new EducationStudentParticipationOutboxWriterStub(),
                unitOfWork: unitOfWork ?? new EducationUnitOfWorkStub(),
                timeProvider: new EducationFixedTimeProvider(
                    StudentProfileSynchronizationTestData.SynchronizedAtUtc));
        }

        private static EnrollStudentCommand CreateCommand(Guid institutionId)
        {
            return new EnrollStudentCommand(
                SimulationHostId: StudentProfileSynchronizationTestData.HostId,
                ResidentId: ResidentId,
                InstitutionId: institutionId,
                Stage: "upper-secondary",
                EnrolledOn: new DateOnly(2048, 5, 1));
        }

        private static StudentProfile CreateProfile()
        {
            return StudentProfileSynchronizationTestData.CreateProfile(
                ResidentId,
                sourceRevision: 1);
        }

        private static EducationInstitution CreateInstitution(int capacity)
        {
            return EducationInstitution.Create(
                id: EducationInstitutionId.New(),
                simulationHostId: new SimulationHostId(StudentProfileSynchronizationTestData.HostId),
                name: "Central school",
                kind: new EducationInstitutionKindKey("school"),
                capacity: capacity);
        }
    }
}
