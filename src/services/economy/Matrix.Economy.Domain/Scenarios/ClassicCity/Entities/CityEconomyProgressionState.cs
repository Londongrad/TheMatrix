using Matrix.BuildingBlocks.Domain;

namespace Matrix.Economy.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityEconomyProgressionState
    {
        private CityEconomyProgressionState() { }

        private CityEconomyProgressionState(
            Guid cityId,
            long lastCompletedTickId,
            DateOnly lastProcessedDate,
            DateTimeOffset updatedAtUtc)
        {
            CityId = GuardHelper.AgainstEmptyGuid(
                id: cityId,
                propertyName: nameof(cityId));
            LastCompletedTickId = lastCompletedTickId >= 0
                ? lastCompletedTickId
                : throw new ArgumentOutOfRangeException(nameof(lastCompletedTickId));
            LastProcessedDate = lastProcessedDate;
            UpdatedAtUtc = updatedAtUtc;
        }

        public Guid CityId { get; private set; }
        public long LastCompletedTickId { get; private set; }
        public DateOnly LastProcessedDate { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static CityEconomyProgressionState Create(
            Guid cityId,
            long lastCompletedTickId,
            DateOnly lastProcessedDate,
            DateTimeOffset updatedAtUtc)
        {
            return new CityEconomyProgressionState(
                cityId: cityId,
                lastCompletedTickId: lastCompletedTickId,
                lastProcessedDate: lastProcessedDate,
                updatedAtUtc: updatedAtUtc);
        }

        public void AdvanceProcessedDate(
            DateOnly processedDate,
            DateTimeOffset updatedAtUtc)
        {
            if (processedDate < LastProcessedDate)
                throw new InvalidOperationException("Economy progression date cannot move backwards.");

            LastProcessedDate = processedDate;
            UpdatedAtUtc = updatedAtUtc;
        }

        public void MarkTickCompleted(
            long tickId,
            DateTimeOffset updatedAtUtc)
        {
            if (tickId < LastCompletedTickId)
                throw new InvalidOperationException("Economy progression tick cannot move backwards.");

            LastCompletedTickId = tickId;
            UpdatedAtUtc = updatedAtUtc;
        }
    }
}
