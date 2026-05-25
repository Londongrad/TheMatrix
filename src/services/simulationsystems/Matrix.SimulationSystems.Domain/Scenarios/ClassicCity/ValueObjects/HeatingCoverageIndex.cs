using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.ValueObjects
{
    /// <summary>
    ///     Normalized heating coverage in the range [0..1], where 0 means citywide heating failure and 1 means stable heat
    ///     availability.
    /// </summary>
    public readonly record struct HeatingCoverageIndex
    {
        public const decimal Min = 0m;
        public const decimal Max = 1m;

        public HeatingCoverageIndex(decimal value)
        {
            Value = Normalize(
                value: value,
                paramName: nameof(Value));
        }

        public decimal Value { get; }

        public static HeatingCoverageIndex From(decimal value)
        {
            return new HeatingCoverageIndex(value);
        }

        public override string ToString()
        {
            return Value.ToString("0.####");
        }

        private static decimal Normalize(
            decimal value,
            string paramName)
        {
            return decimal.Round(
                d: GuardHelper.AgainstOutOfRange(
                    value: value,
                    min: Min,
                    max: Max,
                    errorFactory: ClassicCityDomainErrorsFactory.CityNormalizedIndexOutOfRange,
                    propertyName: paramName),
                decimals: 4,
                mode: MidpointRounding.AwayFromZero);
        }
    }
}
