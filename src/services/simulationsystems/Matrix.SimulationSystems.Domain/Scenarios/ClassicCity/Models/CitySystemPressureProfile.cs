using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Models
{
    /// <summary>
    ///     Scenario-level external pressure/support inputs for daily environmental recalculation.
    ///     All fields are normalized to [0..1].
    /// </summary>
    public sealed class CitySystemPressureProfile
    {
        public CitySystemPressureProfile(
            decimal rainPressure,
            decimal snowPressure,
            decimal stormPressure,
            decimal freezePressure,
            decimal thawRelief,
            decimal drainageSupport,
            decimal snowRemovalSupport,
            decimal roadSupport,
            decimal powerSupport = 0m,
            decimal heatingSupport = 0m,
            decimal waterSupport = 0m,
            decimal sanitationSupport = 0m)
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
            DrainageSupport = NormalizeIndex(
                value: drainageSupport,
                paramName: nameof(drainageSupport));
            SnowRemovalSupport = NormalizeIndex(
                value: snowRemovalSupport,
                paramName: nameof(snowRemovalSupport));
            RoadSupport = NormalizeIndex(
                value: roadSupport,
                paramName: nameof(roadSupport));
            PowerSupport = NormalizeIndex(
                value: powerSupport,
                paramName: nameof(powerSupport));
            HeatingSupport = NormalizeIndex(
                value: heatingSupport,
                paramName: nameof(heatingSupport));
            WaterSupport = NormalizeIndex(
                value: waterSupport,
                paramName: nameof(waterSupport));
            SanitationSupport = NormalizeIndex(
                value: sanitationSupport,
                paramName: nameof(sanitationSupport));
        }

        public decimal RainPressure { get; }
        public decimal SnowPressure { get; }
        public decimal StormPressure { get; }
        public decimal FreezePressure { get; }
        public decimal ThawRelief { get; }
        public decimal DrainageSupport { get; }
        public decimal SnowRemovalSupport { get; }
        public decimal RoadSupport { get; }
        public decimal PowerSupport { get; }
        public decimal HeatingSupport { get; }
        public decimal WaterSupport { get; }
        public decimal SanitationSupport { get; }

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
