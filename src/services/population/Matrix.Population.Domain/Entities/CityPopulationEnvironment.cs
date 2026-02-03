using Matrix.Population.Domain.Enums;
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
            EnsureUtcOffsetMinutes(utcOffsetMinutes);

            CityId = cityId;
            ClimateZone = climateZone;
            Hemisphere = hemisphere;
            UtcOffsetMinutes = utcOffsetMinutes;
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
            EnsureUtcOffsetMinutes(utcOffsetMinutes);

            ClimateZone = climateZone;
            Hemisphere = hemisphere;
            UtcOffsetMinutes = utcOffsetMinutes;
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

        private static void EnsureUtcOffsetMinutes(int value)
        {
            if (value is < -14 * 60 or > 14 * 60)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: "UTC offset minutes must be within [-840, 840].");
        }
    }
}
