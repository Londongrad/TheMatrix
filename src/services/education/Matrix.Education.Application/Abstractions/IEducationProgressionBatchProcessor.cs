using Matrix.Education.Application.Progression;
using Matrix.Simulation.Primitives;

namespace Matrix.Education.Application.Abstractions
{
    /// <summary>
    ///     Processes one simulation tick as a bulk unit. Implementations must use set-based or chunked
    ///     persistence and must not make a remote call for each student.
    /// </summary>
    public interface IEducationProgressionBatchProcessor
    {
        SimulationRuntimeKey RuntimeKey { get; }

        Task<EducationProgressionBatchResult> ProcessAsync(
            EducationProgressionBatch batch,
            CancellationToken cancellationToken = default);
    }
}
