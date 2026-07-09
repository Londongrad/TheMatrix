using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Abstractions
{
    public interface IPatientMedicalRecordRepository
    {
        Task<IReadOnlyList<PatientMedicalRecord>> GetByIdsAsync(
            IReadOnlyCollection<PatientId> patientIds,
            CancellationToken cancellationToken = default);

        Task<PatientPopulationHealthBurden> GetPopulationHealthBurdenAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<PatientCommunityHealthBurden>> GetCommunityHealthBurdensAsync(
            SimulationHostId simulationHostId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<PatientHouseholdId, int>> GetInfectiousPatientCountsByHouseholdAsync(
            SimulationHostId simulationHostId,
            IReadOnlyCollection<PatientHouseholdId> householdIds,
            CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            IReadOnlyCollection<PatientMedicalRecord> records,
            CancellationToken cancellationToken = default);
    }
}
