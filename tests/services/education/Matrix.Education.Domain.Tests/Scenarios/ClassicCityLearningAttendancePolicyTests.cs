using Matrix.Education.Domain.Scenarios.ClassicCity.Attendance;
using Xunit;

namespace Matrix.Education.Domain.Tests.Scenarios;

public sealed class ClassicCityLearningAttendancePolicyTests
{
    private static readonly ClassicCityLearningConditions Baseline = new(18, 70, 25, 100, false,
        1m, 1m, 1m, 1m, 0m, 0m, 0m, false, false, true, 1m);

    [Fact]
    public void Evaluate_PreservesBaselineAndLowerBound()
    {
        var policy = new ClassicCityLearningAttendancePolicy();
        Assert.Equal(1m, policy.Evaluate(Baseline));
        Assert.Equal(0.18m, policy.Evaluate(Baseline with { Energy = 0, Stress = 100, FunctionalCapacity = 0, IsHomeless = true }));
    }

    [Fact]
    public void Evaluate_PreservesCommuteAndChildAdjustments()
    {
        var policy = new ClassicCityLearningAttendancePolicy();
        var conditions = Baseline with { HasCommuteData = true, IsCommuteAccessible = false, CommuteAccessibility = 0.5m };
        Assert.Equal(0.61m, policy.Evaluate(conditions));
        Assert.Equal(0.64m, policy.Evaluate(conditions with { AgeYears = 6 }));
        Assert.Equal(1m, policy.Evaluate(conditions with { HasCommuteData = false }));
    }

    [Fact]
    public void Evaluate_RejectsMalformedFacts()
    {
        var policy = new ClassicCityLearningAttendancePolicy();
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.Evaluate(Baseline with { Energy = 101 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => policy.Evaluate(Baseline with { RoadAccessibility = -1m }));
    }
}
