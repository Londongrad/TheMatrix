using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class CityPopulationWeatherImpactState
    {
        private CityPopulationWeatherImpactState() { }

        private CityPopulationWeatherImpactState(
            CityId cityId,
            DateTimeOffset lastAppliedAtSimTimeUtc,
            DateTimeOffset lastAppliedOccurredOnUtc,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(lastAppliedAtSimTimeUtc, nameof(lastAppliedAtSimTimeUtc));
            EnsureUtc(lastAppliedOccurredOnUtc, nameof(lastAppliedOccurredOnUtc));
            EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

            CityId = cityId;
            LastAppliedAtSimTimeUtc = lastAppliedAtSimTimeUtc;
            LastAppliedOccurredOnUtc = lastAppliedOccurredOnUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        public CityId CityId { get; private set; }
        public DateTimeOffset LastAppliedAtSimTimeUtc { get; private set; }
        public DateTimeOffset LastAppliedOccurredOnUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static CityPopulationWeatherImpactState Create(
            CityId cityId,
            DateTimeOffset lastAppliedAtSimTimeUtc,
            DateTimeOffset lastAppliedOccurredOnUtc,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationWeatherImpactState(
                cityId: cityId,
                lastAppliedAtSimTimeUtc: lastAppliedAtSimTimeUtc,
                lastAppliedOccurredOnUtc: lastAppliedOccurredOnUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public void MarkApplied(
            DateTimeOffset atSimTimeUtc,
            DateTimeOffset occurredOnUtc,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(atSimTimeUtc, nameof(atSimTimeUtc));
            EnsureUtc(occurredOnUtc, nameof(occurredOnUtc));
            EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

            GuardHelper.Ensure(
                condition: atSimTimeUtc >= LastAppliedAtSimTimeUtc,
                value: atSimTimeUtc,
                errorFactory: (value, propertyName) => DomainErrorsFactory.CityPopulationWeatherImpactSimTimeCannotMoveBackwards(
                    value: value,
                    previous: LastAppliedAtSimTimeUtc,
                    propertyName: propertyName),
                propertyName: nameof(atSimTimeUtc));

            GuardHelper.Ensure(
                condition: atSimTimeUtc != LastAppliedAtSimTimeUtc || occurredOnUtc > LastAppliedOccurredOnUtc,
                value: occurredOnUtc,
                errorFactory: (value, propertyName) => DomainErrorsFactory.CityPopulationWeatherImpactOccurredOnCannotMoveBackwards(
                    value: value,
                    previous: LastAppliedOccurredOnUtc,
                    propertyName: propertyName),
                propertyName: nameof(occurredOnUtc));

            LastAppliedAtSimTimeUtc = atSimTimeUtc;
            LastAppliedOccurredOnUtc = occurredOnUtc;
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
