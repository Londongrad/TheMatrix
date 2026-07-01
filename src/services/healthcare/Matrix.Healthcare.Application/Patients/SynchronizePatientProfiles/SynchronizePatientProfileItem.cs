using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Application.Patients.SynchronizePatientProfiles
{
    public sealed record SynchronizePatientProfileItem(
        Guid PatientId,
        DateOnly BirthDate,
        PatientSex Sex,
        bool IsAlive,
        bool IsActive,
        long SourceRevision,
        long LifecycleRevision = 0);
}
