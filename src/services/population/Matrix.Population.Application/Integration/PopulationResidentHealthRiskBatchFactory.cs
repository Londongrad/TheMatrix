using Matrix.Population.Contracts.Events;

namespace Matrix.Population.Application.Integration
{
    public static class PopulationResidentHealthRiskBatchFactory
    {
        public const int DefaultBatchSize = 1000;

        public static PopulationResidentHealthRiskBatchV1[] Build(
            Guid simulationHostId,
            long sourceRevision,
            DateOnly previousDate,
            DateOnly currentDate,
            IReadOnlyCollection<PopulationResidentHealthRiskSnapshot> residents,
            string correlationId,
            DateTimeOffset observedAtUtc,
            int batchSize = DefaultBatchSize)
        {
            if (simulationHostId == Guid.Empty)
                throw new ArgumentException(
                    message: "A simulation host identifier is required.",
                    paramName: nameof(simulationHostId));
            if (sourceRevision < 0)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(sourceRevision),
                    message: "Health risk source revisions cannot be negative.");
            if (currentDate < previousDate)
                throw new ArgumentException(
                    message: "The current health risk date cannot precede the previous date.",
                    paramName: nameof(currentDate));

            ArgumentNullException.ThrowIfNull(residents);
            ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

            if (observedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Health risk observation timestamps must be expressed in UTC.",
                    paramName: nameof(observedAtUtc));
            if (batchSize <= 0 || batchSize > DefaultBatchSize)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(batchSize),
                    message: $"Health risk batch sizes must be between 1 and {DefaultBatchSize}.");

            PopulationResidentHealthRiskV1[] risks = residents
               .OrderBy(resident => resident.ResidentId)
               .Select(Map)
               .ToArray();

            if (risks.Length == 0)
                return [];

            PopulationResidentHealthRiskBatchV1[] batches = risks
               .Chunk(batchSize)
               .Select((chunk, index) => new PopulationResidentHealthRiskBatchV1(
                    SimulationHostId: simulationHostId,
                    SourceRevision: sourceRevision,
                    PreviousDate: previousDate,
                    CurrentDate: currentDate,
                    ObservedAtUtc: observedAtUtc,
                    CorrelationId: correlationId,
                    BatchNumber: index + 1,
                    TotalBatches: 0,
                    Residents: chunk))
               .ToArray();

            for (int index = 0; index < batches.Length; index++)
                batches[index] = batches[index] with
                {
                    TotalBatches = batches.Length
                };

            return batches;
        }

        private static PopulationResidentHealthRiskV1 Map(
            PopulationResidentHealthRiskSnapshot resident)
        {
            ArgumentNullException.ThrowIfNull(resident);
            if (resident.ResidentId == Guid.Empty)
                throw new ArgumentException("A resident identifier is required.", nameof(resident));
            if (!string.Equals(resident.HousingStability, "Unknown", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(resident.HousingStability, "Housed", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(resident.HousingStability, "Unhoused", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"Housing stability '{resident.HousingStability}' is not supported.",
                    nameof(resident));

            return new PopulationResidentHealthRiskV1(
                ResidentId: resident.ResidentId,
                EnergyScore: resident.EnergyScore,
                HappinessScore: resident.HappinessScore,
                StressScore: resident.StressScore,
                SocialNeedScore: resident.SocialNeedScore,
                IsVulnerable: resident.IsVulnerable,
                HousingStability: resident.HousingStability,
                HasStructuredDailyActivity: resident.HasStructuredDailyActivity,
                InfectiousHouseholdContacts: resident.InfectiousHouseholdContacts,
                HouseholdSize: resident.HouseholdSize,
                CaregiverSupportStrength: resident.CaregiverSupportStrength,
                HadAdverseWeatherExposure: resident.HadAdverseWeatherExposure,
                HealthcareSupportStrength: resident.HealthcareSupportStrength,
                PublicHealthRiskStrength: resident.PublicHealthRiskStrength,
                ExternalHealthDelta: resident.ExternalHealthDelta,
                LifecycleRevision: resident.LifecycleRevision,
                CommunityId: resident.CommunityId);
        }
    }
}
