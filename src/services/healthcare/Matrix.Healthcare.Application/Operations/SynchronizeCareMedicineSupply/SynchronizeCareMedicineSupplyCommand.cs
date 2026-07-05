using MediatR;

namespace Matrix.Healthcare.Application.Operations.SynchronizeCareMedicineSupply;

public sealed record SynchronizeCareMedicineSupplyCommand(
    Guid SimulationHostId,
    long SourceRevision,
    decimal StockLevelIndex,
    decimal ShortageRiskIndex,
    DateTimeOffset ObservedAtUtc)
    : IRequest<SynchronizeCareMedicineSupplyResult>;
