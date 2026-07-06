using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Population.Contracts.Events;

namespace Matrix.Healthcare.Integration.Consumers
{
    internal static class PopulationResidentMedicalStateCommandMapper
    {
        internal static InitializePatientMedicalRecordsCommand Map(
            PopulationResidentMedicalStateBatchV1 message)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(message.Residents);

            if (string.IsNullOrWhiteSpace(message.CorrelationId))
                throw new ArgumentException(
                    message: "A resident medical state correlation identifier is required.",
                    paramName: nameof(message));

            if (message.TotalBatches <= 0
                || message.BatchNumber <= 0
                || message.BatchNumber > message.TotalBatches)
                throw new ArgumentException(
                    message: "Resident medical state batch position metadata is invalid.",
                    paramName: nameof(message));

            InitializePatientMedicalRecordItem[] records = message.Residents
                .Select(resident => new InitializePatientMedicalRecordItem(
                    PatientId: resident.ResidentId,
                    HealthScore: resident.HealthScore,
                    LifecycleRevision: resident.LifecycleRevision))
               .ToArray();

            return new InitializePatientMedicalRecordsCommand(
                SimulationHostId: message.SimulationHostId,
                ObservedAtUtc: message.ObservedAtUtc,
                Records: records,
                SourceRevision: message.SourceRevision);
        }

    }
}
