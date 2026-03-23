using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    /// <summary>
    ///     Persisted weather-driven pressure inputs for environmental systems.
    ///     Support coefficients are derived from the current system state and are not stored here.
    /// </summary>
    public sealed class CityWeatherPressureProfile
    {
        private CityWeatherPressureProfile() { }

        public CityWeatherPressureProfile(
            decimal rainPressure,
            decimal snowPressure,
            decimal stormPressure,
            decimal freezePressure,
            decimal thawRelief)
        {
            RainPressure = NormalizeIndex(
                value: rainPressure,
                paramName: nameof(rainPressure));
            SnowPressure = NormalizeIndex(
                value: snowPressure,
                paramName: nameof(snowPressure));
            StormPressure = NormalizeIndex(
                value: stormPressure,
                paramName: nameof(stormPressure));
            FreezePressure = NormalizeIndex(
                value: freezePressure,
                paramName: nameof(freezePressure));
            ThawRelief = NormalizeIndex(
                value: thawRelief,
                paramName: nameof(thawRelief));
        }

        public decimal RainPressure { get; private set; }
        public decimal SnowPressure { get; private set; }
        public decimal StormPressure { get; private set; }
        public decimal FreezePressure { get; private set; }
        public decimal ThawRelief { get; private set; }

        public static CityWeatherPressureProfile Neutral()
        {
            return new CityWeatherPressureProfile(
                rainPressure: 0m,
                snowPressure: 0m,
                stormPressure: 0m,
                freezePressure: 0m,
                thawRelief: 0m);
        }

        private static decimal NormalizeIndex(
            decimal value,
            string paramName)
        {
            return decimal.Round(
                d: GuardHelper.AgainstOutOfRange(
                    value: value,
                    min: 0m,
                    max: 1m,
                    errorFactory: ClassicCityDomainErrorsFactory.CityNormalizedIndexOutOfRange,
                    propertyName: paramName),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
