using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Entities
{
    public sealed class CityPopulationServiceQualityState
    {
        private CityPopulationServiceQualityState() { }

        private CityPopulationServiceQualityState(
            CityId cityId,
            decimal healthcareQualityIndex,
            decimal housingSupportIndex,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            CityId = cityId;
            HealthcareQualityIndex = ValidateIndex(
                value: healthcareQualityIndex,
                paramName: nameof(healthcareQualityIndex));
            HousingSupportIndex = ValidateIndex(
                value: housingSupportIndex,
                paramName: nameof(housingSupportIndex));
            LastEvaluatedAtUtc = EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
            UpdatedAtUtc = EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));
        }

        public CityId CityId { get; private set; }
        public decimal HealthcareQualityIndex { get; private set; }
        public decimal HousingSupportIndex { get; private set; }
        public DateTimeOffset LastEvaluatedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static CityPopulationServiceQualityState Create(
            CityId cityId,
            decimal healthcareQualityIndex,
            decimal housingSupportIndex,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            return new CityPopulationServiceQualityState(
                cityId: cityId,
                healthcareQualityIndex: healthcareQualityIndex,
                housingSupportIndex: housingSupportIndex,
                lastEvaluatedAtUtc: lastEvaluatedAtUtc,
                updatedAtUtc: updatedAtUtc);
        }

        public void ApplySnapshot(
            decimal healthcareQualityIndex,
            decimal housingSupportIndex,
            DateTimeOffset lastEvaluatedAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            HealthcareQualityIndex = ValidateIndex(
                value: healthcareQualityIndex,
                paramName: nameof(healthcareQualityIndex));
            HousingSupportIndex = ValidateIndex(
                value: housingSupportIndex,
                paramName: nameof(housingSupportIndex));
            LastEvaluatedAtUtc = EnsureUtc(
                value: lastEvaluatedAtUtc,
                paramName: nameof(lastEvaluatedAtUtc));
            UpdatedAtUtc = EnsureUtc(
                value: updatedAtUtc,
                paramName: nameof(updatedAtUtc));
        }

        private static decimal ValidateIndex(
            decimal value,
            string paramName)
        {
            if (value is < 0.20m or > 3m)
                throw new ArgumentOutOfRangeException(paramName);

            return decimal.Round(
                d: value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }

        private static DateTimeOffset EnsureUtc(
            DateTimeOffset value,
            string paramName)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: DomainErrorsFactory.TimestampMustBeUtc,
                propertyName: paramName);

            return value;
        }
    }
}
