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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Create_UsesAuthoritativeEconomicsEvenWhenTheyAreExplicitlyNeutral(bool neutral)
        {
            var economics = neutral ? ResidentExternalEconomicProfile.Neutral : new ResidentExternalEconomicProfile(
                ResidentAgeIncomeSchedule.Create((0, 99m)), 7m, 0.1d, 0.5d, -0.1m, 0.05m, 0.05m);
            var participation = CreateParticipation(true, Guid.NewGuid(), "higher") with { Economics = economics };

            var activity = EducationResidentExternalActivityProfileFactory.Create(participation);

            Assert.Same(economics, activity.Economics);
            Assert.Equal(neutral ? 0m : 99m, activity.Economics.TransferIncome.Resolve(18));
            Assert.Equal(neutral ? 1d : 0.5d, activity.Economics.EmploymentAvailabilityFactor);
        }

        [Theory]
        [InlineData(null, 0, 0d)]
        [InlineData("unrecognized", 0, 0d)]
        [InlineData("primary", 1, 0.003d)]
        [InlineData("lower-secondary", 3, 0.006d)]
        [InlineData("upper-secondary", 6, 0.010d)]
        [InlineData("vocational", 10, 0.018d)]
        [InlineData("higher", 14, 0.024d)]
        [InlineData("higher-education", 14, 0.024d)]
        [InlineData("postgraduate", 18, 0.028d)]
        public void Create_MapsEducationEconomicTerms(
            string? completedStage,
            decimal expectedIncomeBonus,
            double expectedOpportunityBonus)
        {
            foreach (bool isEnrolled in new[] { false, true })
            {
                var participation = CreateParticipation(isEnrolled, null, completedStage);
                ResidentExternalEconomicProfile economics =
                    EducationResidentExternalActivityProfileFactory.Create(participation).Economics;

                Assert.Equal(expectedIncomeBonus, economics.EmploymentIncomeBonus);
                Assert.Equal(expectedOpportunityBonus, economics.EmploymentOpportunityBonus);
                Assert.Equal(isEnrolled ? 4m : 0m, economics.TransferIncome.Resolve(16));
                Assert.Equal(isEnrolled ? 10m : 0m, economics.TransferIncome.Resolve(17));
                Assert.Equal(isEnrolled ? 10m : 0m, economics.TransferIncome.Resolve(66));
                Assert.Equal(isEnrolled ? 0d : 1d, economics.EmploymentAvailabilityFactor);
                Assert.Equal(isEnrolled ? -0.03m : 0m, economics.RetailStoreSpendShareAdjustment);
                Assert.Equal(isEnrolled ? -0.01m : 0m, economics.ServiceSpendShareAdjustment);
                Assert.Equal(isEnrolled ? 0.04m : 0m, economics.MunicipalSpendShareAdjustment);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Create_ReusesEconomicTermsAcrossResidents(bool isEnrolled)
        {
            var first = CreateParticipation(isEnrolled, Guid.NewGuid(), "higher");
            var second = CreateParticipation(isEnrolled, Guid.NewGuid(), "higher");

            Assert.Same(
                EducationResidentExternalActivityProfileFactory.Create(first).Economics,
                EducationResidentExternalActivityProfileFactory.Create(second).Economics);
        }

        [Fact]
        public void Create_WithoutEnrollmentOrQualification_UsesNeutralEconomics()
        {
            var participation = CreateParticipation(false, null, null);

            Assert.Same(ResidentExternalEconomicProfile.Neutral,
                EducationResidentExternalActivityProfileFactory.Create(participation).Economics);
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
