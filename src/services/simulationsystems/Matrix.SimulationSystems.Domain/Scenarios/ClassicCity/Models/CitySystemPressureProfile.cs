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
            decimal roadSupport)
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
        }

        public decimal RainPressure { get; }
        public decimal SnowPressure { get; }
        public decimal StormPressure { get; }
        public decimal FreezePressure { get; }
        public decimal ThawRelief { get; }
        public decimal DrainageSupport { get; }
        public decimal SnowRemovalSupport { get; }
        public decimal RoadSupport { get; }

        private static decimal NormalizeIndex(
            decimal value,
            string paramName)
        {
            if (value is < 0m or > 1m)
                throw new ArgumentOutOfRangeException(paramName);

            return decimal.Round(
                d: value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
