using Matrix.Education.Application.Abstractions;
using Matrix.Education.Contracts.Events;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Scenarios.ClassicCity.Participation;

public sealed class ClassicCityEducationRoutinePolicy : IEducationParticipationRoutinePolicy
{
    private static readonly EducationDailyRoutineV1 Enrolled = new(new(8 * 60, 15 * 60, 0b_0111110, "moderate"));
    private static readonly EducationDailyRoutineV1 NotEnrolled = new(null);

    public SimulationRuntimeKey RuntimeKey { get; } = new(new SimulationScenarioKey("classic-city"), new SimulationHostTypeKey("city"));

    public EducationDailyRoutineV1 Resolve(bool isEnrolled, string? activeStage) => isEnrolled ? Enrolled : NotEnrolled;
}
