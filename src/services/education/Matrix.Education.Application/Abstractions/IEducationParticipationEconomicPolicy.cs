using Matrix.Education.Contracts.Events;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Abstractions;

public interface IEducationParticipationEconomicPolicy
{
    SimulationRuntimeKey RuntimeKey { get; }
    EducationEconomicEffectsV1 Resolve(bool isEnrolled, string? completedStage);
}
