using MediatR;

namespace Matrix.Population.Application.Integration.Education.ApplyEducationParticipation
{
    public sealed record ApplyEducationParticipationCommand(
        Guid SimulationHostId,
        Guid IntegrationMessageId,
        string ConsumerName,
        DateOnly SnapshotDate,
        DateTimeOffset OccurredAtUtc,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<StudentEducationParticipationInput> Students)
        : IRequest<ApplyEducationParticipationResult>;
}
