namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects
{
    /// <summary>
    ///     Normalized snow accumulation pressure in the range [0..1], where 0 means clear ground and 1 means critical accumulation.
    /// </summary>
    public readonly record struct SnowAccumulationIndex
    {
        public const decimal Min = 0m;
        public const decimal Max = 1m;

        public SnowAccumulationIndex(decimal value)
        {
            Value = Normalize(
                value: value,
                paramName: nameof(Value));
        }

        public decimal Value { get; }

        public static SnowAccumulationIndex From(decimal value)
        {
            return new SnowAccumulationIndex(value);
        }

        public override string ToString()
        {
            return Value.ToString("0.####");
        }

        private static decimal Normalize(
            decimal value,
            string paramName)
        {
            if (value is < Min or > Max)
                throw new ArgumentOutOfRangeException(paramName);

            return decimal.Round(
                d: value,
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
