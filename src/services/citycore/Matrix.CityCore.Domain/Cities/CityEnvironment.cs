using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Cities.Enums;
using Matrix.CityCore.Domain.Errors;

namespace Matrix.CityCore.Domain.Cities
{
    /// <summary>
    ///     Long-lived city context that influences climate, seasons, and local time.
    /// </summary>
    public sealed record class CityEnvironment
    {
        private CityEnvironment() { }

        private CityEnvironment(
            ClimateZone climateZone,
            Hemisphere hemisphere,
            CityUtcOffset utcOffset)
        {
            ClimateZone = climateZone;
            Hemisphere = hemisphere;
            UtcOffset = utcOffset;
        }

        public ClimateZone ClimateZone { get; private set; }
        public Hemisphere Hemisphere { get; private set; }
        public CityUtcOffset UtcOffset { get; private set; }

        public static CityEnvironment Create(
            ClimateZone climateZone,
            Hemisphere hemisphere,
            CityUtcOffset utcOffset)
        {
            GuardHelper.AgainstInvalidEnum(
                value: climateZone,
                propertyName: nameof(ClimateZone));

            GuardHelper.AgainstInvalidEnum(
                value: hemisphere,
                propertyName: nameof(Hemisphere));

            if (utcOffset == default)
                throw DomainErrorsFactory.InvalidCityEnvironment(
                    reason: "UTC offset is required.",
                    propertyName: nameof(UtcOffset));

            return new CityEnvironment(
                climateZone: climateZone,
                hemisphere: hemisphere,
                utcOffset: utcOffset);
        }
    }
}