using MediatR;

namespace Matrix.Healthcare.Application.Operations.SynchronizeCareServiceQuality;

public sealed record SynchronizeCareServiceQualityCommand(
    Guid SimulationHostId,
    decimal QualityMultiplier,
    DateTimeOffset ObservedAtUtc)
    : IRequest<SynchronizeCareServiceQualityResult>;
