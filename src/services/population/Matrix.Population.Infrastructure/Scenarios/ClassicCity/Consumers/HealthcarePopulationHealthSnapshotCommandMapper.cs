using Matrix.Healthcare.Contracts.Events;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyHealthcarePressureSnapshot;

namespace Matrix.Population.Infrastructure.Scenarios.ClassicCity.Consumers;

internal static class HealthcarePopulationHealthSnapshotCommandMapper
{
    internal static ApplyHealthcarePressureSnapshotCommand Map(
        HealthcarePopulationHealthSnapshotV1 message,
        Guid messageId,
        string consumerName)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new ApplyHealthcarePressureSnapshotCommand(
            CityId: message.SimulationHostId,
            IntegrationMessageId: messageId,
            ConsumerName: consumerName,
            SourceRevision: message.SourceRevision,
            CurrentDate: message.CurrentDate,
            PatientCount: message.PatientCount,
            ActiveIllnessCount: message.ActiveIllnessCount,
            SevereIllnessCount: message.SevereIllnessCount,
            MedicalLoadIndex: message.MedicalLoadIndex,
            TriagePressureIndex: message.TriagePressureIndex,
            RecoverySupportIndex: message.RecoverySupportIndex,
            OccurredAtUtc: message.OccurredAtUtc);
    }
}
