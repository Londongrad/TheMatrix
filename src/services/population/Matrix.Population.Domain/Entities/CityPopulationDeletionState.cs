using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class CityPopulationDeletionState
    {
        private CityPopulationDeletionState() { }

        private CityPopulationDeletionState(
            CityId cityId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(deletedAtUtc, nameof(deletedAtUtc));
            EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

            CityId = cityId;
            DeletedAtUtc = deletedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        public CityId CityId { get; private set; }
        public DateTimeOffset DeletedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static CityPopulationDeletionState Create(
            CityId cityId,
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationDeletionState(
                cityId: cityId,
                deletedAtUtc: deletedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public void MarkDeleted(
            DateTimeOffset deletedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(deletedAtUtc, nameof(deletedAtUtc));
            EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

            if (deletedAtUtc < DeletedAtUtc)
                return;

            DeletedAtUtc = deletedAtUtc;
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
