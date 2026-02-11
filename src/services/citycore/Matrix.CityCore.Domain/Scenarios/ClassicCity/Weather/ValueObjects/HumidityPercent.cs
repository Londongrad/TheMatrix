using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects
{
    /// <summary>
    ///     Relative air humidity percentage in the range [0..100].
    /// </summary>
    public readonly record struct HumidityPercent
    {
        public const decimal Min = 0m;
        public const decimal Max = 100m;

        public HumidityPercent(decimal value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: Min,
                max: Max,
                errorFactory: ClassicCityDomainErrorsFactory.HumidityPercentOutOfRange,
                propertyName: nameof(Value));
        }

        public decimal Value { get; }

        public static HumidityPercent From(decimal value)
        {
            return new HumidityPercent(value);
        }

        public override string ToString()
        {
            return Value.ToString("0.##");
        }
    }
}
