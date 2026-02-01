using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class CityPopulationProgressionState
    {
        private CityPopulationProgressionState() { }

        private CityPopulationProgressionState(
            CityId cityId,
            long lastProcessedTickId,
            DateOnly lastProcessedDate,
            DateTimeOffset updatedAtUtc)
        {
            if (lastProcessedTickId < 0)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(lastProcessedTickId),
                    message: "Last processed tick id cannot be negative.");

            EnsureUtc(updatedAtUtc);

            CityId = cityId;
            LastProcessedTickId = lastProcessedTickId;
            LastProcessedDate = lastProcessedDate;
            UpdatedAtUtc = updatedAtUtc;
        }

        public CityId CityId { get; private set; }
        public long LastProcessedTickId { get; private set; }
        public DateOnly LastProcessedDate { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static CityPopulationProgressionState Create(
            CityId cityId,
            long lastProcessedTickId,
            DateOnly lastProcessedDate,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationProgressionState(
                cityId: cityId,
                lastProcessedTickId: lastProcessedTickId,
                lastProcessedDate: lastProcessedDate,
                updatedAtUtc: updatedAtUtc);
        }

        public void MarkProcessed(
            long tickId,
            DateOnly processedDate,
            DateTimeOffset updatedAtUtc)
        {
            if (tickId < 0)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(tickId),
                    message: "Tick id cannot be negative.");

            if (tickId < LastProcessedTickId)
                throw new InvalidOperationException(
                    $"Tick id '{tickId}' cannot move backwards from '{LastProcessedTickId}'.");

            if (processedDate < LastProcessedDate)
                throw new InvalidOperationException(
                    $"Processed date '{processedDate}' cannot move backwards from '{LastProcessedDate}'.");

            EnsureUtc(updatedAtUtc);

            LastProcessedTickId = tickId;
            LastProcessedDate = processedDate;
            UpdatedAtUtc = updatedAtUtc;
        }

        private static void EnsureUtc(DateTimeOffset value)
        {
            if (value.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Timestamps must be in UTC.",
                    paramName: nameof(value));
        }
    }
}
