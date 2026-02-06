using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class CityPopulationEnvironment
    {
        private CityPopulationEnvironment() { }

        private CityPopulationEnvironment(
            CityId cityId,
            PopulationClimateZone climateZone,
            PopulationHemisphere hemisphere,
            int utcOffsetMinutes,
            DateTimeOffset createdAtUtc,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(createdAtUtc, nameof(createdAtUtc));
            EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

            CityId = cityId;
            ClimateZone = GuardHelper.AgainstInvalidEnum(
                value: climateZone,
                propertyName: nameof(ClimateZone));
            Hemisphere = GuardHelper.AgainstInvalidEnum(
                value: hemisphere,
                propertyName: nameof(Hemisphere));
            UtcOffsetMinutes = GuardHelper.AgainstOutOfRange(
                value: utcOffsetMinutes,
                min: -14 * 60,
                max: 14 * 60,
                errorFactory: DomainErrorsFactory.CityPopulationUtcOffsetMinutesOutOfRange,
                propertyName: nameof(UtcOffsetMinutes));
            CreatedAtUtc = createdAtUtc;
            UpdatedAtUtc = updatedAtUtc;
        }

        public CityId CityId { get; private set; }
        public PopulationClimateZone ClimateZone { get; private set; }
        public PopulationHemisphere Hemisphere { get; private set; }
        public int UtcOffsetMinutes { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; }

        public static CityPopulationEnvironment Create(
            CityId cityId,
            PopulationClimateZone climateZone,
            PopulationHemisphere hemisphere,
            int utcOffsetMinutes,
            DateTimeOffset createdAtUtc)
        {
            return new CityPopulationEnvironment(
                cityId: cityId,
                climateZone: climateZone,
                hemisphere: hemisphere,
                utcOffsetMinutes: utcOffsetMinutes,
                createdAtUtc: createdAtUtc,
                updatedAtUtc: createdAtUtc);
        }

        public void Sync(
            PopulationClimateZone climateZone,
            PopulationHemisphere hemisphere,
            int utcOffsetMinutes,
            DateTimeOffset updatedAtUtc)
        {
            EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));

            ClimateZone = GuardHelper.AgainstInvalidEnum(
                value: climateZone,
                propertyName: nameof(ClimateZone));
            Hemisphere = GuardHelper.AgainstInvalidEnum(
                value: hemisphere,
                propertyName: nameof(Hemisphere));
            UtcOffsetMinutes = GuardHelper.AgainstOutOfRange(
                value: utcOffsetMinutes,
                min: -14 * 60,
                max: 14 * 60,
                errorFactory: DomainErrorsFactory.CityPopulationUtcOffsetMinutesOutOfRange,
                propertyName: nameof(UtcOffsetMinutes));
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
