using Matrix.Population.Application.Integration;
using Matrix.Population.Application.Integration.Education;
using Matrix.Population.Domain.Models;
using Xunit;

namespace Matrix.Population.Application.Tests.Integration.Education
{
    public sealed class EducationResidentExternalActivityProfileFactoryTests
    {
        [Fact]
        public void Create_ActiveEnrollment_MapsStructuredDestination()
        {
            Guid anchorId = Guid.NewGuid();
            EducationParticipationProjection participation = CreateParticipation(
                isEnrolled: true,
                institutionAnchorId: anchorId,
                completedStage: "upper-secondary");

            ResidentExternalActivityProfile profile =
                EducationResidentExternalActivityProfileFactory.Create(participation);

            Assert.True(profile.HasStructuredActivity);
            Assert.Equal(3, profile.ResidentLifecycleRevision);
            Assert.Equal(TimeSpan.FromHours(8), profile.Routine.StructuredActivityStart);
            Assert.Equal(TimeSpan.FromHours(15), profile.Routine.StructuredActivityEnd);
            Assert.Equal(PersonStructuredActivityLoad.Moderate, profile.Routine.StructuredActivityLoad);
            Assert.Equal(anchorId, profile.DestinationAnchorId);
            Assert.Equal("EducationCommute", profile.CommutePurpose);
            Assert.Equal(
                ResidentWorkforceQualificationTier.General,
                profile.WorkforceQualification);
        }

        [Theory]
        [InlineData("primary", ResidentWorkforceQualificationTier.Entry)]
        [InlineData("vocational", ResidentWorkforceQualificationTier.Skilled)]
        [InlineData("higher", ResidentWorkforceQualificationTier.Professional)]
        [InlineData("postgraduate", ResidentWorkforceQualificationTier.Specialist)]
        public void Create_CompletedStage_MapsNeutralWorkforceTier(
            string completedStage,
            ResidentWorkforceQualificationTier expected)
        {
            EducationParticipationProjection participation = CreateParticipation(
                isEnrolled: false,
                institutionAnchorId: null,
                completedStage: completedStage);

            ResidentExternalActivityProfile profile =
                EducationResidentExternalActivityProfileFactory.Create(participation);

            Assert.False(profile.HasStructuredActivity);
            Assert.Null(profile.DestinationAnchorId);
            Assert.Null(profile.CommutePurpose);
            Assert.Equal(expected, profile.WorkforceQualification);
        }

        [Fact]
        public void Create_MissingParticipation_ReturnsNone()
        {
            Assert.Same(
                ResidentExternalActivityProfile.None,
                EducationResidentExternalActivityProfileFactory.Create(null));
        }

        private static EducationParticipationProjection CreateParticipation(
            bool isEnrolled,
            Guid? institutionAnchorId,
            string? completedStage)
        {
            return new EducationParticipationProjection(
                SimulationHostId: Guid.NewGuid(),
                ResidentId: Guid.NewGuid(),
                ParticipationRevision: 1,
                ResidentLifecycleRevision: 3,
                IsEnrolled: isEnrolled,
                ActiveStage: isEnrolled ? "higher" : null,
                InstitutionId: isEnrolled ? Guid.NewGuid() : null,
                InstitutionAnchorId: institutionAnchorId,
                EnrolledOn: isEnrolled ? new DateOnly(2048, 5, 1) : null,
                CompletedStage: completedStage,
                CompletedStageOn: completedStage is null ? null : new DateOnly(2047, 6, 30),
                SnapshotDate: new DateOnly(2048, 5, 2),
                OccurredAtUtc: DateTimeOffset.Parse("2048-05-02T10:00:00+00:00"),
                UpdatedAtUtc: DateTimeOffset.Parse("2048-05-02T10:00:00+00:00"));
        }
    }
}
