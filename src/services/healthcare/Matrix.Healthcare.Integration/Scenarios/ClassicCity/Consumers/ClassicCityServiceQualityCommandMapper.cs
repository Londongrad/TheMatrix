using Matrix.Healthcare.Application.Operations.SynchronizeCareServiceQuality;
using Matrix.ScenarioContracts.ClassicCity.IntegrationEvents.Population;

namespace Matrix.Healthcare.Integration.Scenarios.ClassicCity.Consumers;

internal static class ClassicCityServiceQualityCommandMapper
{
    internal static SynchronizeCareServiceQualityCommand Map(
        ClassicCityServiceQualitySnapshotV1 message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new SynchronizeCareServiceQualityCommand(
            SimulationHostId: message.CityId,
            QualityMultiplier: message.HealthcareQualityIndex,
            ObservedAtUtc: message.OccurredAtUtc);
    }
}
