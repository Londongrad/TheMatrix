using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class CityPopulationArchiveState
    {
        private CityPopulationArchiveState() { }

        private CityPopulationArchiveState(
            CityId cityId,
            DateTimeOffset archivedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(archivedAtUtc, nameof(archivedAtUtc));
            EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

            CityId = cityId;
            ArchivedAtUtc = archivedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        public CityId CityId { get; private set; }
        public DateTimeOffset ArchivedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static CityPopulationArchiveState Create(
            CityId cityId,
            DateTimeOffset archivedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationArchiveState(
                cityId: cityId,
                archivedAtUtc: archivedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public void MarkArchived(
            DateTimeOffset archivedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(archivedAtUtc, nameof(archivedAtUtc));
            EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

            if (archivedAtUtc < ArchivedAtUtc)
                return;

            ArchivedAtUtc = archivedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        private static void EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            if (value.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Timestamps must be in UTC.",
                    paramName: paramName);
        }
    }
}
