using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationArchiveState
    {
        private CityPopulationArchiveState() { }

        private CityPopulationArchiveState(
            CityId cityId,
            DateTimeOffset archivedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(
                value: archivedAtUtc,
                paramName: nameof(archivedAtUtc));
            EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));

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
            EnsureUtc(
                value: archivedAtUtc,
                paramName: nameof(archivedAtUtc));
            EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));

            if (archivedAtUtc < ArchivedAtUtc)
                return;

            ArchivedAtUtc = archivedAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        private static void EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: DomainErrorsFactory.TimestampMustBeUtc,
                propertyName: paramName);
        }
    }
}
