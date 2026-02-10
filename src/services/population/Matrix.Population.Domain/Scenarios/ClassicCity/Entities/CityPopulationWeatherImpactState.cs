using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
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
            EnsureUtc(
                value: lastAppliedAtSimTimeUtc,
                paramName: nameof(lastAppliedAtSimTimeUtc));
            EnsureUtc(
                value: lastAppliedOccurredOnUtc,
                paramName: nameof(lastAppliedOccurredOnUtc));
            EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));

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
            EnsureUtc(
                value: atSimTimeUtc,
                paramName: nameof(atSimTimeUtc));
            EnsureUtc(
                value: occurredOnUtc,
                paramName: nameof(occurredOnUtc));
            EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));

            GuardHelper.Ensure(
                condition: atSimTimeUtc >= LastAppliedAtSimTimeUtc,
                value: atSimTimeUtc,
                errorFactory: (
                    value,
                    propertyName) => DomainErrorsFactory.CityPopulationWeatherImpactSimTimeCannotMoveBackwards(
                    value: value,
                    previous: LastAppliedAtSimTimeUtc,
                    propertyName: propertyName));

            GuardHelper.Ensure(
                condition: atSimTimeUtc != LastAppliedAtSimTimeUtc || occurredOnUtc > LastAppliedOccurredOnUtc,
                value: occurredOnUtc,
                errorFactory: (
                    value,
                    propertyName) => DomainErrorsFactory.CityPopulationWeatherImpactOccurredOnCannotMoveBackwards(
                    value: value,
                    previous: LastAppliedOccurredOnUtc,
                    propertyName: propertyName));

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
