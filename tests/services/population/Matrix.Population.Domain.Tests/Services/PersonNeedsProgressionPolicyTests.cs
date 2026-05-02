using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Services;

public sealed class PersonNeedsProgressionPolicyTests
{
    [Fact]
    public void Calculate_WhenPersonIsDeadOrIntervalDoesNotAdvance_ReturnsNone()
    {
        PersonNeedsProgressionPolicy policy = new();
        Matrix.Population.Domain.Entities.Person person = PopulationTestData.CreateAdultPerson();
        person.Die(new DateOnly(2048, 5, 1));

        PersonNeedsProgressionEffect deadEffect = policy.Calculate(
            person: person,
            fromSimTimeUtc: new DateTimeOffset(2048, 5, 1, 0, 0, 0, TimeSpan.Zero),
            toSimTimeUtc: new DateTimeOffset(2048, 5, 1, 2, 0, 0, TimeSpan.Zero),
            utcOffsetMinutes: 0);
        PersonNeedsProgressionEffect noAdvanceEffect = policy.Calculate(
            person: PopulationTestData.CreateAdultPerson(),
            fromSimTimeUtc: new DateTimeOffset(2048, 5, 1, 2, 0, 0, TimeSpan.Zero),
            toSimTimeUtc: new DateTimeOffset(2048, 5, 1, 2, 0, 0, TimeSpan.Zero),
            utcOffsetMinutes: 0);

        Assert.Equal(PersonNeedsProgressionEffect.None, deadEffect);
        Assert.Equal(PersonNeedsProgressionEffect.None, noAdvanceEffect);
    }

    [Fact]
    public void Calculate_WhenLocalTimeFallsIntoSleepWindow_IncreasesEnergyAndReducesStress()
    {
        PersonNeedsProgressionPolicy policy = new();
        Matrix.Population.Domain.Entities.Person person = PopulationTestData.CreateAdultPerson();

        PersonNeedsProgressionEffect effect = policy.Calculate(
            person: person,
            fromSimTimeUtc: new DateTimeOffset(2048, 5, 1, 13, 0, 0, TimeSpan.Zero),
            toSimTimeUtc: new DateTimeOffset(2048, 5, 1, 15, 0, 0, TimeSpan.Zero),
            utcOffsetMinutes: 600);

        Assert.Equal(12, effect.EnergyDelta);
        Assert.Equal(-6, effect.StressDelta);
        Assert.Equal(0, effect.SocialNeedDelta);
        Assert.Equal(0, effect.HealthDelta);
        Assert.Equal(0, effect.HappinessDelta);
        Assert.True(effect.HasAnyEffect);
    }

    [Fact]
    public void Calculate_WhenStudentIsInStructuredActivity_DrainsEnergyAndLowersSocialNeed()
    {
        PersonNeedsProgressionPolicy policy = new();
        Matrix.Population.Domain.Entities.Person student = PopulationTestData.CreateAdultPerson();
        student.StartStudying(
            currentDate: new DateOnly(2048, 5, 1),
            institutionId: PopulationTestData.CreateEducationInstitutionId(),
            institutionAnchorId: PopulationTestData.CreateCityAnchorId());

        PersonNeedsProgressionEffect effect = policy.Calculate(
            person: student,
            fromSimTimeUtc: new DateTimeOffset(2048, 5, 1, 9, 0, 0, TimeSpan.Zero),
            toSimTimeUtc: new DateTimeOffset(2048, 5, 1, 11, 0, 0, TimeSpan.Zero),
            utcOffsetMinutes: 0);

        Assert.Equal(-7, effect.EnergyDelta);
        Assert.Equal(5, effect.StressDelta);
        Assert.Equal(-2, effect.SocialNeedDelta);
        Assert.Equal(0, effect.HealthDelta);
        Assert.Equal(0, effect.HappinessDelta);
    }
}
