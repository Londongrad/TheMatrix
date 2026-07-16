using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Services
{
    public sealed class PersonNeedsProgressionPolicyTests
    {
        [Fact]
        public void Calculate_WhenPersonIsDeadOrIntervalDoesNotAdvance_ReturnsNone()
        {
            PersonNeedsProgressionPolicy policy = new();
            Person person = PopulationTestData.CreateAdultPerson();
            person.Die(
                new DateOnly(
                    year: 2048,
                    month: 5,
                    day: 1));

            PersonNeedsProgressionEffect deadEffect = policy.Calculate(
                person: person,
                fromSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 0,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                toSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 2,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                utcOffsetMinutes: 0,
                routineProfile: PersonRoutineProfile.Unstructured);
            PersonNeedsProgressionEffect noAdvanceEffect = policy.Calculate(
                person: PopulationTestData.CreateAdultPerson(),
                fromSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 2,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                toSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 2,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                utcOffsetMinutes: 0,
                routineProfile: PersonRoutineProfile.Unstructured);

            Assert.Equal(
                expected: PersonNeedsProgressionEffect.None,
                actual: deadEffect);
            Assert.Equal(
                expected: PersonNeedsProgressionEffect.None,
                actual: noAdvanceEffect);
        }

        [Fact]
        public void Calculate_WhenLocalTimeFallsIntoSleepWindow_IncreasesEnergyAndReducesStress()
        {
            PersonNeedsProgressionPolicy policy = new();
            Person person = PopulationTestData.CreateAdultPerson();

            PersonNeedsProgressionEffect effect = policy.Calculate(
                person: person,
                fromSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 13,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                toSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 15,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                utcOffsetMinutes: 600,
                routineProfile: PersonRoutineProfile.Unstructured);

            Assert.Equal(
                expected: 12,
                actual: effect.EnergyDelta);
            Assert.Equal(
                expected: -6,
                actual: effect.StressDelta);
            Assert.Equal(
                expected: 0,
                actual: effect.SocialNeedDelta);
            Assert.Equal(
                expected: 0,
                actual: effect.HealthDelta);
            Assert.Equal(
                expected: 0,
                actual: effect.HappinessDelta);
            Assert.True(effect.HasAnyEffect);
        }

        [Fact]
        public void Calculate_WhenModerateStructuredActivityIsActive_DrainsEnergyAndLowersSocialNeed()
        {
            PersonNeedsProgressionPolicy policy = new();
            Person person = PopulationTestData.CreateAdultPerson();
            PersonRoutineProfile routineProfile = PersonRoutineProfile.Structured(
                activityStart: TimeSpan.FromHours(8),
                activityEnd: TimeSpan.FromHours(15),
                activityLoad: PersonStructuredActivityLoad.Moderate);

            PersonNeedsProgressionEffect effect = policy.Calculate(
                person: person,
                fromSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 9,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                toSimTimeUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 1,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero),
                utcOffsetMinutes: 0,
                routineProfile: routineProfile);

            Assert.Equal(
                expected: -7,
                actual: effect.EnergyDelta);
            Assert.Equal(
                expected: 5,
                actual: effect.StressDelta);
            Assert.Equal(
                expected: -2,
                actual: effect.SocialNeedDelta);
            Assert.Equal(
                expected: 0,
                actual: effect.HealthDelta);
            Assert.Equal(
                expected: 0,
                actual: effect.HappinessDelta);
        }
    }
}
