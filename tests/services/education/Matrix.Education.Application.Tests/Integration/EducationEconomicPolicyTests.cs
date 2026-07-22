using Matrix.Education.Application.Integration;
using Matrix.Education.Application.Scenarios.ClassicCity.Participation;
using Matrix.Simulation.Primitives;
using Xunit;

namespace Matrix.Education.Application.Tests.Integration;

public sealed class EducationEconomicPolicyTests
{
    [Theory]
    [InlineData(null, 0, 0d)]
    [InlineData("unknown", 0, 0d)]
    [InlineData("primary", 1, 0.003d)]
    [InlineData("lower-secondary", 3, 0.006d)]
    [InlineData("upper-secondary", 6, 0.010d)]
    [InlineData("vocational", 10, 0.018d)]
    [InlineData("higher", 14, 0.024d)]
    [InlineData("higher-education", 14, 0.024d)]
    [InlineData("postgraduate", 18, 0.028d)]
    public void ClassicCity_PreservesExistingTermsAndReusesProfiles(string? stage, decimal income, double opportunity)
    {
        var policy = new ClassicCityEducationEconomicPolicy();
        foreach (bool enrolled in new[] { false, true })
        {
            var effects = policy.Resolve(enrolled, stage);
            Assert.Same(effects, policy.Resolve(enrolled, stage));
            Assert.Equal(income, effects.EmploymentIncomeBonus);
            Assert.Equal(opportunity, effects.EmploymentOpportunityBonus);
            Assert.Equal(enrolled ? 0d : 1d, effects.EmploymentAvailabilityFactor);
            Assert.Equal(enrolled ? 4m : 0m, effects.TransferIncome[0].DailyIncome);
            Assert.Equal(enrolled ? -0.03m : 0m, effects.RetailStoreSpendShareAdjustment);
            Assert.Equal(enrolled ? -0.01m : 0m, effects.ServiceSpendShareAdjustment);
            Assert.Equal(enrolled ? 0.04m : 0m, effects.MunicipalSpendShareAdjustment);
            if (enrolled)
            {
                Assert.Equal(17, effects.TransferIncome[1].MinimumAge);
                Assert.Equal(10m, effects.TransferIncome[1].DailyIncome);
            }
        }
    }

    [Fact]
    public void Registry_DoesNotFallBackToCityForAnotherRuntime()
    {
        var policy = new ClassicCityEducationEconomicPolicy();
        var registry = new EducationEconomicPolicyRegistry([policy]);
        Assert.Same(policy, registry.Resolve(policy.RuntimeKey));
        Assert.Throws<NotSupportedException>(() => registry.Resolve(new(new SimulationScenarioKey("other"), new SimulationHostTypeKey("city"))));
        Assert.Throws<NotSupportedException>(() => registry.Resolve(new(new SimulationScenarioKey("classic-city"), new SimulationHostTypeKey("network"))));
        Assert.Throws<ArgumentException>(() => registry.Resolve(default));
        Assert.Throws<InvalidOperationException>(() => new EducationEconomicPolicyRegistry([policy, policy]));
    }
}
