using MediatR;

namespace Matrix.Education.Application.Progression.AdvanceEducationProgression
{
    public sealed record AdvanceEducationProgressionCommand(
        Guid SimulationHostId,
        long TickId,
        DateTimeOffset FromSimTimeUtc,
        DateTimeOffset ToSimTimeUtc)
        : IRequest<AdvanceEducationProgressionResult>;
}
