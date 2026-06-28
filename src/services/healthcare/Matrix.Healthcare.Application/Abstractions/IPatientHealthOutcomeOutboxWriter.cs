using Matrix.Healthcare.Application.Patients.AdvancePatientHealth;

namespace Matrix.Healthcare.Application.Abstractions
{
    public interface IPatientHealthOutcomeOutboxWriter
    {
        Task AddAsync(
            PatientHealthOutcomeBatch batch,
            CancellationToken cancellationToken = default);
    }
}
