using Matrix.Education.Application.Integration;
using Matrix.Education.Application.Scenarios.ClassicCity.Participation;
using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Education.Application.Tests.Integration;

public sealed class EducationRoutinePolicyTests
{
    [Fact]
    public void ClassicCity_PreservesHoursWeekdaysAndLoadWithoutAllocatingPerStudent()
    {
        var policy = new ClassicCityEducationRoutinePolicy();
        var routine = policy.Resolve(true, "primary");
        Assert.Equal(480, routine.StructuredActivity!.StartMinuteOfDay);
        Assert.Equal(900, routine.StructuredActivity.EndMinuteOfDay);
        Assert.Equal(62, routine.StructuredActivity.DaysOfWeekMask);
        Assert.Equal("moderate", routine.StructuredActivity.Load);
        Assert.Same(routine, policy.Resolve(true, "higher"));
        Assert.Null(policy.Resolve(false, null).StructuredActivity);
        Assert.Same(policy.Resolve(false, null), policy.Resolve(false, "primary"));
    }

    [Fact]
    public void Registry_RejectsMissingAndAmbiguousRuntimeInsteadOfUsingCityRules()
    {
        var policy = new ClassicCityEducationRoutinePolicy();
        var registry = new EducationRoutinePolicyRegistry([policy]);
        Assert.Same(policy, registry.Resolve(policy.RuntimeKey));
        Assert.Throws<NotSupportedException>(() => registry.Resolve(new(new SimulationScenarioKey("other"), new SimulationHostTypeKey("city"))));
        Assert.Throws<NotSupportedException>(() => registry.Resolve(new(new SimulationScenarioKey("classic-city"), new SimulationHostTypeKey("network"))));
        Assert.Throws<ArgumentException>(() => registry.Resolve(default));
        Assert.Throws<InvalidOperationException>(() => new EducationRoutinePolicyRegistry([policy, policy]));
    }
}
