using Matrix.Education.Contracts.Events;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Abstractions;

public interface IEducationParticipationRoutinePolicy
{
    SimulationRuntimeKey RuntimeKey { get; }
    EducationDailyRoutineV1 Resolve(bool isEnrolled, string? activeStage);
}
