namespace Matrix.Healthcare.Domain.Operations;

public readonly record struct CareQualityMultiplier
{
    public const decimal Minimum = 0m;
    public const decimal Maximum = 2m;

    public CareQualityMultiplier(decimal value)
    {
        if (value is < Minimum or > Maximum)
            throw new ArgumentOutOfRangeException(
                paramName: nameof(value),
                message: $"Care quality multipliers must be between {Minimum} and {Maximum}.");

        Value = value;
    }

    public decimal Value { get; }

    public static CareQualityMultiplier Baseline => new(1m);
}
