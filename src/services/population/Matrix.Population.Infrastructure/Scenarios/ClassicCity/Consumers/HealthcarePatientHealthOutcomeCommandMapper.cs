using Matrix.Healthcare.Contracts.Events;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyPatientHealthOutcomes;
using Matrix.Population.Domain.Enums;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    internal static class HealthcarePatientHealthOutcomeCommandMapper
    {
        internal static ApplyPatientHealthOutcomesCommand Map(
            HealthcarePatientHealthOutcomeBatchV1 message,
            Guid messageId,
            string consumerName)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(message.Patients);

            PatientHealthOutcomeInput[] patients = message.Patients
               .Select(patient => new PatientHealthOutcomeInput(
                    PatientId: patient.PatientId,
                    HealthScore: patient.HealthScore,
                    CurrentIllnessKind: ParseOptional<IllnessKind>(
                        patient.CurrentIllnessKind,
                        nameof(patient.CurrentIllnessKind)),
                    CurrentIllnessSeverity: ParseOptional<IllnessSeverity>(
                        patient.CurrentIllnessSeverity,
                        nameof(patient.CurrentIllnessSeverity)),
                    DiagnosedOn: patient.DiagnosedOn,
                    LastRecoveredOn: patient.LastRecoveredOn,
                    HappinessDelta: patient.HappinessDelta,
                    EnergyDelta: patient.EnergyDelta,
                    StressDelta: patient.StressDelta,
                    LifecycleRevision: patient.LifecycleRevision,
                    FunctionalCapacityScore: patient.FunctionalCapacityScore))
               .ToArray();

            if (patients.Any(patient =>
                    patient.CurrentIllnessKind.HasValue != patient.CurrentIllnessSeverity.HasValue))
                throw new ArgumentException(
                    "Healthcare illness kinds and severities must be supplied together.",
                    nameof(message));

            return new ApplyPatientHealthOutcomesCommand(
                CityId: message.SimulationHostId,
                IntegrationMessageId: messageId,
                ConsumerName: consumerName,
                SourceRevision: message.SourceRevision,
                CurrentDate: message.CurrentDate,
                OccurredAtUtc: message.OccurredAtUtc,
                CorrelationId: message.CorrelationId,
                BatchNumber: message.BatchNumber,
                TotalBatches: message.TotalBatches,
                Patients: patients);
        }

        private static TEnum? ParseOptional<TEnum>(string? value, string fieldName)
            where TEnum : struct, Enum
        {
            if (value is null)
                return null;
            if (Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
                && Enum.IsDefined(parsed))
                return parsed;

            throw new ArgumentException(
                $"Healthcare value '{value}' is not a supported {fieldName}.",
                fieldName);
        }
    }
}
