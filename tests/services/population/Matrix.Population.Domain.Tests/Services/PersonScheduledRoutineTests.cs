using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Services;
using Matrix.Population.Domain.Tests.TestSupport;
using Xunit;

namespace Matrix.Population.Domain.Tests.Services;

public sealed class PersonScheduledRoutineTests
{
    [Fact]
    public void Structured_ValidatesDaysAndKeepsWeekdayDefault()
    {
        var routine = PersonRoutineProfile.Structured(TimeSpan.FromHours(8), TimeSpan.FromHours(15), PersonStructuredActivityLoad.Moderate);
        Assert.True(routine.IsScheduledOn(DayOfWeek.Monday));
        Assert.False(routine.IsScheduledOn(DayOfWeek.Saturday));
        Assert.False(PersonRoutineProfile.Unstructured.IsScheduledOn(DayOfWeek.Monday));
        foreach (var days in new[] { PersonRoutineDays.None, (PersonRoutineDays)128, (PersonRoutineDays)(-1) })
            Assert.Throws<ArgumentOutOfRangeException>(() => PersonRoutineProfile.Structured(TimeSpan.Zero,
                TimeSpan.FromHours(1), PersonStructuredActivityLoad.Moderate, days));
    }

    [Fact]
    public void Calculate_UsesLocalScheduledDayInsteadOfHardcodedWeekdays()
    {
        var person = PopulationTestData.CreateAdultPerson();
        var policy = new PersonNeedsProgressionPolicy();
        // 2048-05-02 is Saturday. The same local hour on Monday provides the activity baseline.
        var saturday = new DateTimeOffset(2048, 5, 2, 5, 0, 0, TimeSpan.Zero);
        var scheduled = PersonRoutineProfile.Structured(TimeSpan.FromHours(8), TimeSpan.FromHours(15),
            PersonStructuredActivityLoad.Moderate, PersonRoutineDays.Saturday);
        var weekday = PersonRoutineProfile.Structured(TimeSpan.FromHours(8), TimeSpan.FromHours(15), PersonStructuredActivityLoad.Moderate);
        Assert.Equal(policy.Calculate(person, saturday.AddDays(2), saturday.AddDays(2).AddHours(1), 180, weekday),
            policy.Calculate(person, saturday, saturday.AddHours(1), 180, scheduled));
        Assert.Equal(policy.Calculate(person, saturday, saturday.AddHours(1), 180, PersonRoutineProfile.Unstructured),
            policy.Calculate(person, saturday, saturday.AddHours(1), 180, weekday));
    }

    [Fact]
    public void Calculate_ExplicitLateActivityOverridesDefaultSleepAndEndsAtMidnight()
    {
        var person = PopulationTestData.CreateAdultPerson();
        var policy = new PersonNeedsProgressionPolicy();
        var evening = new DateTimeOffset(2048, 5, 2, 23, 0, 0, TimeSpan.Zero);
        var nightRoutine = PersonRoutineProfile.Structured(TimeSpan.FromHours(23), TimeSpan.FromDays(1),
            PersonStructuredActivityLoad.Moderate, PersonRoutineDays.Saturday);
        var dayRoutine = PersonRoutineProfile.Structured(TimeSpan.FromHours(8), TimeSpan.FromHours(15),
            PersonStructuredActivityLoad.Moderate, PersonRoutineDays.Saturday);
        Assert.Equal(policy.Calculate(person, evening.AddHours(-15), evening.AddHours(-14), 0, dayRoutine),
            policy.Calculate(person, evening, evening.AddHours(1), 0, nightRoutine));
        Assert.Equal(policy.Calculate(person, evening.AddHours(1), evening.AddHours(2), 0, PersonRoutineProfile.Unstructured),
            policy.Calculate(person, evening.AddHours(1), evening.AddHours(2), 0, nightRoutine));
    }
}
