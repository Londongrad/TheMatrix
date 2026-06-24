using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Application.Abstractions
{
    public interface IPatientProfileRepository
    {
        Task<IReadOnlyList<PatientProfile>> GetByIdsAsync(
            IReadOnlyCollection<PatientId> patientIds,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IReadOnlyCollection<PatientProfile> profiles,
            CancellationToken cancellationToken = default);
    }
}
