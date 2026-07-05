namespace Matrix.Healthcare.Domain.Operations;

public readonly record struct CareAvailabilityIndex
{
    public const decimal Minimum = 0m;
    public const decimal Maximum = 1m;

    public CareAvailabilityIndex(decimal value)
    {
        if (value is < Minimum or > Maximum)
            throw new ArgumentOutOfRangeException(
                paramName: nameof(value),
                message: $"Care availability indexes must be between {Minimum} and {Maximum}.");

        Value = value;
    }

    public decimal Value { get; }

    public static CareAvailabilityIndex Full => new(1m);
    public static CareAvailabilityIndex None => new(0m);
}
