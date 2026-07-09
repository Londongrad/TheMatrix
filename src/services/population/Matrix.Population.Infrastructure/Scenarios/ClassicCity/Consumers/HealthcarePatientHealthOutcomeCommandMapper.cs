using Matrix.Healthcare.Contracts.Events;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyPatientHealthOutcomes;

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
                    HappinessDelta: patient.HappinessDelta,
                    EnergyDelta: patient.EnergyDelta,
                    StressDelta: patient.StressDelta,
                    LifecycleRevision: patient.LifecycleRevision,
                    FunctionalCapacityScore: patient.FunctionalCapacityScore))
               .ToArray();

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
    }
}
