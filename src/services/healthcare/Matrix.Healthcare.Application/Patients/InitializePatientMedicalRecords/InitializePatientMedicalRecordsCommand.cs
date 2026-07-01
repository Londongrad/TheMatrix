using MediatR;

namespace Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords
{
    public sealed record InitializePatientMedicalRecordsCommand(
        Guid SimulationHostId,
        DateTimeOffset ObservedAtUtc,
        IReadOnlyList<InitializePatientMedicalRecordItem> Records,
        long SourceRevision = 0)
        : IRequest<InitializePatientMedicalRecordsResult>;
}
