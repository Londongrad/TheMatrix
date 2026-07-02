using Matrix.Healthcare.Domain.Care;
using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Application.Abstractions;

public interface IPatientCareNeedRepository
{
    Task<IReadOnlyList<PatientCareNeed>> GetByPatientIdsAsync(
        IReadOnlyCollection<PatientId> patientIds,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<PatientCareNeed> careNeeds,
        CancellationToken cancellationToken = default);
}
