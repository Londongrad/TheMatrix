using Matrix.Healthcare.Application.Patients.InitializePatientMedicalRecords;
using Matrix.Healthcare.Domain.Patients;
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
                    CurrentIllnessKind: MapOptionalEnum<IllnessKind>(resident.CurrentIllnessKind),
                    CurrentIllnessSeverity: MapOptionalEnum<IllnessSeverity>(resident.CurrentIllnessSeverity),
                    DiagnosedOn: resident.DiagnosedOn,
                    LastRecoveredOn: resident.LastRecoveredOn))
               .ToArray();

            return new InitializePatientMedicalRecordsCommand(
                SimulationHostId: message.SimulationHostId,
                ObservedAtUtc: message.ObservedAtUtc,
                Records: records);
        }

        private static TEnum? MapOptionalEnum<TEnum>(string? value)
            where TEnum : struct, Enum
        {
            if (value is null)
                return null;

            if (Enum.TryParse(value, ignoreCase: true, out TEnum result) && Enum.IsDefined(result))
                return result;

            throw new ArgumentException(
                message: $"Population medical value '{value}' is not a supported {typeof(TEnum).Name}.",
                paramName: nameof(value));
        }
    }
}
