using MediatR;

namespace Matrix.Healthcare.Application.Patients.AdvancePatientHealth
{
    public sealed record AdvancePatientHealthCommand(
        Guid SimulationHostId,
        long SourceRevision,
        DateOnly PreviousDate,
        DateOnly CurrentDate,
        DateTimeOffset ObservedAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyList<AdvancePatientHealthRiskItem> Patients)
        : IRequest<AdvancePatientHealthResult>;
}
