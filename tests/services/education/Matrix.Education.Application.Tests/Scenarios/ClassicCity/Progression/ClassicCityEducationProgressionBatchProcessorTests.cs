using Matrix.Education.Application.Integration;
using Matrix.Education.Application.Progression;
using Matrix.Education.Application.Scenarios.ClassicCity.Progression;
using Matrix.Education.Application.Tests.TestSupport;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Education.Application.Tests.Scenarios.ClassicCity.Progression
{
    public sealed class ClassicCityEducationProgressionBatchProcessorTests
    {
        private static readonly SimulationHostId HostId = new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"));
        private static readonly DateTimeOffset CurrentUtc =
            new(2048, 6, 1, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task Process_ChildWithoutHistory_InfersBaselineAndStartsRequiredStage()
        {
            StudentProfile profile = CreateProfile(CurrentUtc.Date.AddYears(-8));
            EducationInstitution school = CreateInstitution("School", capacity: 10);
            var enrollmentRepository = new StudentEnrollmentRepositoryStub();
            ClassicCityEducationProgressionBatchProcessor processor = CreateProcessor(
                [profile],
                enrollmentRepository,
                [school]);

            EducationProgressionBatchResult result = await processor.ProcessAsync(CreateBatch());

            Assert.Equal(ClassicCityEducationStageCatalog.Preschool, profile.CompletedStage);
            StudentEnrollment enrollment = Assert.Single(enrollmentRepository.Added);
            Assert.Equal(ClassicCityEducationStageCatalog.Primary, enrollment.Stage);
            Assert.Equal(1, school.CurrentEnrollmentCount);
            Assert.Equal(1, result.StudentProfilesEvaluated);
            Assert.Equal(1, result.EnrollmentsStarted);
            Assert.Equal(1, result.InstitutionsUpdated);
            Assert.Equal(1, profile.ParticipationRevision);
            Assert.True(Assert.Single(result.ParticipationChanges).IsEnrolled);
        }

        [Fact]
        public async Task Process_CompletedCompulsoryStage_ClosesAndStartsNextStage()
        {
            StudentProfile profile = CreateProfile(CurrentUtc.Date.AddYears(-13));
            profile.RecordStageCompletion(
                ClassicCityEducationStageCatalog.Preschool,
                profile.BirthDate.AddYears(7));
            EducationInstitution school = CreateInstitution("School", capacity: 10);
            StudentEnrollment primary = CreateEnrollment(
                profile,
                school,
                ClassicCityEducationStageCatalog.Primary,
                profile.BirthDate.AddYears(7));
            Assert.True(school.TryReserveSeats(1));
            var enrollmentRepository = new StudentEnrollmentRepositoryStub(primary);
            ClassicCityEducationProgressionBatchProcessor processor = CreateProcessor(
                [profile],
                enrollmentRepository,
                [school]);

            EducationProgressionBatchResult result = await processor.ProcessAsync(CreateBatch());

            Assert.Equal(EnrollmentStatus.Completed, primary.Status);
            Assert.Equal(ClassicCityEducationStageCatalog.Primary, profile.CompletedStage);
            StudentEnrollment next = Assert.Single(enrollmentRepository.Added);
            Assert.Equal(ClassicCityEducationStageCatalog.LowerSecondary, next.Stage);
            Assert.Equal(1, school.CurrentEnrollmentCount);
            Assert.Equal(1, result.EnrollmentsCompleted);
            Assert.Equal(1, result.EnrollmentsStarted);
            EducationStudentParticipationChange change = Assert.Single(result.ParticipationChanges);
            Assert.True(change.IsEnrolled);
            Assert.Equal(ClassicCityEducationStageCatalog.LowerSecondary.Value, change.ActiveStage);
            Assert.Equal(ClassicCityEducationStageCatalog.Primary.Value, change.CompletedStage);
        }

        [Fact]
        public async Task Process_UnavailableStudent_WithdrawsActiveEnrollment()
        {
            StudentProfile profile = CreateProfile(CurrentUtc.Date.AddYears(-10));
            Assert.True(profile.TryDeactivate(sourceRevision: 2, synchronizedAtUtc: CurrentUtc));
            EducationInstitution school = CreateInstitution("School", capacity: 10);
            StudentEnrollment enrollment = CreateEnrollment(
                profile,
                school,
                ClassicCityEducationStageCatalog.Primary,
                new DateOnly(2047, 9, 1));
            Assert.True(school.TryReserveSeats(1));
            var enrollmentRepository = new StudentEnrollmentRepositoryStub(enrollment);
            ClassicCityEducationProgressionBatchProcessor processor = CreateProcessor(
                [profile],
                enrollmentRepository,
                [school]);

            EducationProgressionBatchResult result = await processor.ProcessAsync(CreateBatch());

            Assert.Equal(EnrollmentStatus.Withdrawn, enrollment.Status);
            Assert.Equal(0, school.CurrentEnrollmentCount);
            Assert.Empty(enrollmentRepository.Added);
            Assert.Equal(1, result.EnrollmentsWithdrawn);
            Assert.False(Assert.Single(result.ParticipationChanges).IsEnrolled);
        }

        [Fact]
        public async Task Process_WhenSchoolHasNoCapacity_DoesNotCreateEnrollment()
        {
            StudentProfile profile = CreateProfile(CurrentUtc.Date.AddYears(-5));
            EducationInstitution school = CreateInstitution("School", capacity: 1);
            Assert.True(school.TryReserveSeats(1));
            var enrollmentRepository = new StudentEnrollmentRepositoryStub();
            ClassicCityEducationProgressionBatchProcessor processor = CreateProcessor(
                [profile],
                enrollmentRepository,
                [school]);

            EducationProgressionBatchResult result = await processor.ProcessAsync(CreateBatch());

            Assert.Empty(enrollmentRepository.Added);
            Assert.Equal(0, result.EnrollmentsStarted);
            Assert.Equal(0, result.InstitutionsUpdated);
            Assert.Empty(result.ParticipationChanges);
        }

        private static ClassicCityEducationProgressionBatchProcessor CreateProcessor(
            IReadOnlyList<StudentProfile> profiles,
            StudentEnrollmentRepositoryStub enrollmentRepository,
            IReadOnlyCollection<EducationInstitution> institutions)
        {
            return new ClassicCityEducationProgressionBatchProcessor(
                studentProfileRepository: new StudentProfileRepositoryStub(profiles),
                enrollmentRepository: enrollmentRepository,
                institutionRepository: new EducationInstitutionRepositoryStub(institutions),
                progressionPolicy: new ClassicCityEducationProgressionPolicy(),
                institutionSelectionPolicy: new ClassicCityEducationInstitutionSelectionPolicy());
        }

        private static EducationProgressionBatch CreateBatch()
        {
            return EducationProgressionBatch.Create(
                runtimeKey: new SimulationRuntimeKey(
                    scenarioKey: new SimulationScenarioKey("classic-city"),
                    hostTypeKey: new SimulationHostTypeKey("city")),
                simulationHostId: HostId,
                tickId: 42,
                fromSimTimeUtc: CurrentUtc.AddMinutes(-1),
                toSimTimeUtc: CurrentUtc);
        }

        private static StudentProfile CreateProfile(DateTime birthDate)
        {
            return StudentProfile.Register(
                residentId: new ResidentId(Guid.NewGuid()),
                simulationHostId: HostId,
                birthDate: DateOnly.FromDateTime(birthDate),
                isAlive: true,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: CurrentUtc.AddDays(-1));
        }

        private static EducationInstitution CreateInstitution(string kind, int capacity)
        {
            return EducationInstitution.Create(
                id: EducationInstitutionId.New(),
                simulationHostId: HostId,
                name: $"{kind} institution",
                kind: new EducationInstitutionKindKey(kind),
                capacity: capacity);
        }

        private static StudentEnrollment CreateEnrollment(
            StudentProfile profile,
            EducationInstitution institution,
            EducationStageKey stage,
            DateOnly enrolledOn)
        {
            return StudentEnrollment.Enroll(
                id: EnrollmentId.New(),
                simulationHostId: HostId,
                residentId: profile.ResidentId,
                institutionId: institution.EducationInstitutionId,
                stage: stage,
                enrolledOn: enrolledOn);
        }
    }
}
