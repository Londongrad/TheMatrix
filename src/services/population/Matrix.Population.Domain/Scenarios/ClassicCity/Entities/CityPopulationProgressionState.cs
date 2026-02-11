using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
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
            GuardHelper.AgainstNegativeNumber(
                value: lastProcessedTickId,
                errorFactory: ClassicCityDomainErrorsFactory.CityPopulationTickIdCannotBeNegative);

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
            GuardHelper.AgainstNegativeNumber(
                value: tickId,
                errorFactory: ClassicCityDomainErrorsFactory.CityPopulationTickIdCannotBeNegative);

            GuardHelper.Ensure(
                condition: tickId >= LastProcessedTickId,
                value: tickId,
                errorFactory: (
                    value,
                    propertyName) => ClassicCityDomainErrorsFactory.CityPopulationTickIdCannotMoveBackwards(
                    value: value,
                    previous: LastProcessedTickId,
                    propertyName: propertyName));

            GuardHelper.Ensure(
                condition: processedDate >= LastProcessedDate,
                value: processedDate,
                errorFactory: (
                    value,
                    propertyName) => ClassicCityDomainErrorsFactory.CityPopulationProcessedDateCannotMoveBackwards(
                    value: value,
                    previous: LastProcessedDate,
                    propertyName: propertyName));

            EnsureUtc(updatedAtUtc);

            LastProcessedTickId = tickId;
            LastProcessedDate = processedDate;
            UpdatedAtUtc = updatedAtUtc;
        }

        private static void EnsureUtc(DateTimeOffset value)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: DomainErrorsFactory.TimestampMustBeUtc);
        }
    }
}
