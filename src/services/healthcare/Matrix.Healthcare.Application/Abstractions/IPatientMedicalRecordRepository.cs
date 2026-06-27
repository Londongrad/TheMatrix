using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Application.Abstractions
{
    public interface IPatientMedicalRecordRepository
    {
        Task<IReadOnlyList<PatientMedicalRecord>> GetByIdsAsync(
            IReadOnlyCollection<PatientId> patientIds,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IReadOnlyCollection<PatientMedicalRecord> records,
            CancellationToken cancellationToken = default);
    }
}
