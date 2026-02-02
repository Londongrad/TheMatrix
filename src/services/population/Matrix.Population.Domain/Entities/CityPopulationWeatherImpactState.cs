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

            if (atSimTimeUtc < LastAppliedAtSimTimeUtc)
                throw new InvalidOperationException(
                    $"Weather impact sim time '{atSimTimeUtc:O}' cannot move backwards from '{LastAppliedAtSimTimeUtc:O}'.");

            if (atSimTimeUtc == LastAppliedAtSimTimeUtc &&
                occurredOnUtc <= LastAppliedOccurredOnUtc)
                throw new InvalidOperationException(
                    $"Weather impact occurrence '{occurredOnUtc:O}' cannot move backwards from '{LastAppliedOccurredOnUtc:O}' at the same sim time.");

            LastAppliedAtSimTimeUtc = atSimTimeUtc;
            LastAppliedOccurredOnUtc = occurredOnUtc;
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
