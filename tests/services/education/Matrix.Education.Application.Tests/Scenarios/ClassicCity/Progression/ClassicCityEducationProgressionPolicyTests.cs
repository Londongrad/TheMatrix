using Matrix.Education.Application.Scenarios.ClassicCity.Progression;
using Matrix.Education.Domain.Programs;
using Matrix.Education.Domain.Simulation;
using Matrix.Education.Domain.Students;
using Xunit;

namespace Matrix.Education.Application.Tests.Scenarios.ClassicCity.Progression
{
    public sealed class ClassicCityEducationProgressionPolicyTests
    {
        private readonly ClassicCityEducationProgressionPolicy _policy = new();

        [Theory]
        [InlineData(7, "preschool")]
        [InlineData(13, "primary")]
        [InlineData(16, "lower-secondary")]
        [InlineData(18, "upper-secondary")]
        public void TryResolveInferredBaseline_UsesCompulsoryAgeThresholds(
            int age,
            string expectedStage)
        {
            DateOnly currentDate = new(2048, 6, 1);
            StudentProfile profile = CreateProfile(currentDate.AddYears(-age));

            bool resolved = _policy.TryResolveInferredBaseline(
                profile,
                currentDate,
                out EducationStageKey stage,
                out DateOnly completedOn);

            Assert.True(resolved);
            Assert.Equal(expectedStage, stage.Value);
            Assert.Equal(profile.BirthDate.AddYears(age), completedOn);
        }

        [Fact]
        public void ResolveNextEnrollmentStage_EnrollsChildIntoRequiredStage()
        {
            DateOnly currentDate = new(2048, 6, 1);
            StudentProfile profile = CreateProfile(currentDate.AddYears(-8));
            profile.RecordStageCompletion(
                ClassicCityEducationStageCatalog.Preschool,
                profile.BirthDate.AddYears(7));

            EducationStageKey? stage = _policy.ResolveNextEnrollmentStage(profile, currentDate);

            Assert.Equal(ClassicCityEducationStageCatalog.Primary, stage);
        }

        [Fact]
        public void ResolveNextEnrollmentStage_PostSecondaryReviewIsStableWithinWindow()
        {
            DateOnly currentDate = new(2048, 6, 1);
            StudentProfile profile = CreateProfile(
                birthDate: currentDate.AddYears(-20),
                residentId: Guid.Parse("11111111-2222-3333-4444-555555555555"));
            profile.RecordStageCompletion(
                ClassicCityEducationStageCatalog.UpperSecondary,
                profile.BirthDate.AddYears(18));

            EducationStageKey? first = _policy.ResolveNextEnrollmentStage(profile, currentDate);
            EducationStageKey? repeated = _policy.ResolveNextEnrollmentStage(
                profile,
                currentDate.AddDays(1));

            Assert.Equal(first, repeated);
        }

        [Theory]
        [InlineData("preschool", 7)]
        [InlineData("primary", 13)]
        [InlineData("lower-secondary", 16)]
        [InlineData("upper-secondary", 18)]
        public void ResolveCompletionDate_UsesCompulsoryCompletionAge(
            string stage,
            int completionAge)
        {
            StudentProfile profile = CreateProfile(new DateOnly(2040, 2, 1));

            DateOnly? completionDate = _policy.ResolveCompletionDate(
                profile,
                new EducationStageKey(stage),
                enrolledOn: new DateOnly(2043, 9, 1));

            Assert.Equal(profile.BirthDate.AddYears(completionAge), completionDate);
        }

        [Fact]
        public void ResolveCompletionDate_UsesProgramDurationForHigherEducation()
        {
            StudentProfile profile = CreateProfile(new DateOnly(2028, 2, 1));
            var enrolledOn = new DateOnly(2048, 9, 1);

            DateOnly? completionDate = _policy.ResolveCompletionDate(
                profile,
                ClassicCityEducationStageCatalog.Higher,
                enrolledOn);

            Assert.Equal(enrolledOn.AddYears(4), completionDate);
        }

        private static StudentProfile CreateProfile(
            DateOnly birthDate,
            Guid? residentId = null)
        {
            return StudentProfile.Register(
                residentId: new ResidentId(residentId ?? Guid.NewGuid()),
                simulationHostId: new SimulationHostId(Guid.NewGuid()),
                birthDate: birthDate,
                isAlive: true,
                isActive: true,
                sourceRevision: 1,
                synchronizedAtUtc: new DateTimeOffset(2048, 1, 1, 0, 0, 0, TimeSpan.Zero));
        }
    }
}
