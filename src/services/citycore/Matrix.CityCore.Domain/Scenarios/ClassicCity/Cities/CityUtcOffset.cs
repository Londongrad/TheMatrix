using Matrix.CityCore.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities
{
    /// <summary>
    ///     City UTC offset constrained to real-world timezone bounds.
    /// </summary>
    public readonly record struct CityUtcOffset
    {
        public const int MinMinutes = -12 * 60;
        public const int MaxMinutes = 14 * 60;
        public const int StepMinutes = 15;

        public CityUtcOffset(TimeSpan value)
        {
            int totalMinutes = checked((int)value.TotalMinutes);

            if (value.Ticks %
                TimeSpan.FromMinutes(StepMinutes)
                   .Ticks !=
                0)
                throw ClassicCityDomainErrorsFactory.CityUtcOffsetMustAlignToStep(
                    valueMinutes: totalMinutes,
                    stepMinutes: StepMinutes,
                    propertyName: nameof(Value));

            if (totalMinutes < MinMinutes || totalMinutes > MaxMinutes)
                throw ClassicCityDomainErrorsFactory.CityUtcOffsetOutOfRange(
                    valueMinutes: totalMinutes,
                    minMinutes: MinMinutes,
                    maxMinutes: MaxMinutes,
                    propertyName: nameof(Value));

            Value = value;
        }

        public TimeSpan Value { get; }

        public int TotalMinutes => checked((int)Value.TotalMinutes);

        public static CityUtcOffset FromMinutes(int totalMinutes)
        {
            return new CityUtcOffset(TimeSpan.FromMinutes(totalMinutes));
        }

        public override string ToString()
        {
            string sign = Value < TimeSpan.Zero
                ? "-"
                : "+";

            TimeSpan absolute = Value.Duration();
            return $"{sign}{absolute:hh\\:mm}";
        }
    }
}
