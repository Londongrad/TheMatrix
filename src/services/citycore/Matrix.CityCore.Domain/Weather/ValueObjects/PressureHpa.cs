using Matrix.BuildingBlocks.Domain;
using Matrix.CityCore.Domain.Errors;

namespace Matrix.CityCore.Domain.Weather.ValueObjects
{
    /// <summary>
    ///     Atmospheric pressure in hectopascals.
    /// </summary>
    public readonly record struct PressureHpa
    {
        public const decimal Min = 870m;
        public const decimal Max = 1085m;

        public PressureHpa(decimal value)
        {
            Value = GuardHelper.AgainstOutOfRange(
                value: value,
                min: Min,
                max: Max,
                errorFactory: DomainErrorsFactory.PressureHpaOutOfRange,
                propertyName: nameof(Value));
        }

        public decimal Value { get; }

        public static PressureHpa From(decimal value)
        {
            return new PressureHpa(value);
        }

        public override string ToString()
        {
            return Value.ToString("0.##");
        }
    }
}
