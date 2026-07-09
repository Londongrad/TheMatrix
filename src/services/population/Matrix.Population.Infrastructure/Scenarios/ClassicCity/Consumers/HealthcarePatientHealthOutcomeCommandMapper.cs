using Matrix.Healthcare.Contracts.Events;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyResidentVitalStateOutcomes;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers
{
    internal static class HealthcarePatientHealthOutcomeCommandMapper
    {
        internal static ApplyResidentVitalStateOutcomesCommand Map(
            HealthcarePatientHealthOutcomeBatchV1 message,
            Guid messageId,
            string consumerName)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(message.Patients);

            ResidentVitalStateOutcomeInput[] residents = message.Patients
               .Select(patient => new ResidentVitalStateOutcomeInput(
                    ResidentId: patient.PatientId,
                    HealthScore: patient.HealthScore,
                    HappinessDelta: patient.HappinessDelta,
                    EnergyDelta: patient.EnergyDelta,
                    StressDelta: patient.StressDelta,
                    LifecycleRevision: patient.LifecycleRevision,
                    FunctionalCapacityScore: patient.FunctionalCapacityScore))
               .ToArray();

            return new ApplyResidentVitalStateOutcomesCommand(
                CityId: message.SimulationHostId,
                IntegrationMessageId: messageId,
                ConsumerName: consumerName,
                SourceRevision: message.SourceRevision,
                CurrentDate: message.CurrentDate,
                OccurredAtUtc: message.OccurredAtUtc,
                CorrelationId: message.CorrelationId,
                BatchNumber: message.BatchNumber,
                TotalBatches: message.TotalBatches,
                Residents: residents);
        }
    }
}
