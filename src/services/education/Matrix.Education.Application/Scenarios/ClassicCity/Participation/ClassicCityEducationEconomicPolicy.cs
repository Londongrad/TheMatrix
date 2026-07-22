using Matrix.Education.Application.Abstractions;
using Matrix.Education.Contracts.Events;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Scenarios.ClassicCity.Participation;

public sealed class ClassicCityEducationEconomicPolicy : IEducationParticipationEconomicPolicy
{
    private static readonly IReadOnlyList<EducationAgeIncomeBandV1> StudentIncome = Array.AsReadOnly(
        new[] { new EducationAgeIncomeBandV1(0, 4m), new EducationAgeIncomeBandV1(17, 10m) });
    private static readonly IReadOnlyList<EducationAgeIncomeBandV1> NoIncome = Array.AsReadOnly(
        new[] { new EducationAgeIncomeBandV1(0, 0m) });
    private static readonly (decimal Income, double Opportunity)[] QualificationBenefits =
        [(0m, 0d), (1m, 0.003d), (3m, 0.006d), (6m, 0.010d), (10m, 0.018d), (14m, 0.024d), (18m, 0.028d)];
    private static readonly EducationEconomicEffectsV1[] Enrolled = QualificationBenefits.Select(benefit => Create(benefit, true)).ToArray();
    private static readonly EducationEconomicEffectsV1[] NotEnrolled = QualificationBenefits.Select(benefit => Create(benefit, false)).ToArray();

    public SimulationRuntimeKey RuntimeKey { get; } = new(new SimulationScenarioKey("classic-city"), new SimulationHostTypeKey("city"));

    public EducationEconomicEffectsV1 Resolve(bool isEnrolled, string? completedStage)
    {
        int qualification = completedStage switch
        {
            "primary" => 1,
            "lower-secondary" => 2,
            "upper-secondary" => 3,
            "vocational" => 4,
            "higher" or "higher-education" => 5,
            "postgraduate" => 6,
            _ => 0
        };
        return (isEnrolled ? Enrolled : NotEnrolled)[qualification];
    }

    private static EducationEconomicEffectsV1 Create((decimal Income, double Opportunity) benefit, bool enrolled) => new(
        enrolled ? StudentIncome : NoIncome, benefit.Income, benefit.Opportunity, enrolled ? 0d : 1d,
        enrolled ? -0.03m : 0m, enrolled ? -0.01m : 0m, enrolled ? 0.04m : 0m);
}
