using Matrix.Education.Application.Integration;
using Matrix.Education.Domain.Enrollments;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Application.Tests.Integration
{
    public sealed class EducationStudentParticipationChangeTests
    {
        [Fact]
        public void Capture_ActiveEnrollment_MapsOwnedParticipationState()
        {
            SimulationHostId hostId = new(Guid.NewGuid());
            ResidentId residentId = new(Guid.NewGuid());
            EducationInstitution institution = EducationInstitution.Create(
                id: new EducationInstitutionId(Guid.NewGuid()),
                simulationHostId: hostId,
                name: "North School",
                kind: new EducationInstitutionKindKey("school"),
                capacity: 200,
                locationAnchorId: new LocationAnchorId(Guid.NewGuid()));
            StudentProfile profile = CreateProfile(hostId, residentId);
            profile.RecordParticipationChange();
            StudentEnrollment enrollment = StudentEnrollment.Enroll(
                id: EnrollmentId.New(),
                simulationHostId: hostId,
                residentId: residentId,
                institutionId: institution.EducationInstitutionId,
                stage: new EducationStageKey("primary"),
                enrolledOn: new DateOnly(2026, 9, 1));

            EducationStudentParticipationChange change =
                EducationStudentParticipationChange.Capture(profile, enrollment, institution);

            Assert.True(change.IsEnrolled);
            Assert.Equal(1, change.ParticipationRevision);
            Assert.Equal("primary", change.ActiveStage);
            Assert.Equal(institution.EducationInstitutionId.Value, change.InstitutionId);
            Assert.Equal(institution.LocationAnchorId!.Value.Value, change.InstitutionAnchorId);
        }

        [Fact]
        public void Capture_NoActiveEnrollment_MapsCompletedAttainment()
        {
            StudentProfile profile = CreateProfile(
                new SimulationHostId(Guid.NewGuid()),
                new ResidentId(Guid.NewGuid()));
            profile.RecordStageCompletion(
                new EducationStageKey("upper-secondary"),
                new DateOnly(2026, 6, 30));
            profile.RecordParticipationChange();

            EducationStudentParticipationChange change =
                EducationStudentParticipationChange.Capture(profile);

            Assert.False(change.IsEnrolled);
            Assert.Equal("upper-secondary", change.CompletedStage);
            Assert.Equal(new DateOnly(2026, 6, 30), change.CompletedStageOn);
            Assert.Null(change.InstitutionId);
        }

        private static StudentProfile CreateProfile(
            SimulationHostId hostId,
            ResidentId residentId)
        {
            return StudentProfile.Register(
                residentId,
                hostId,
                new DateOnly(2015, 1, 1),
                isAlive: true,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        }
    }
}
