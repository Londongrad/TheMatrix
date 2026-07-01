namespace Matrix.Healthcare.Domain.Facilities
{
    public readonly record struct CareFacilityKindKey
    {
        public const int MaxLength = 64;

        public CareFacilityKindKey(string value)
        {
            string normalized = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException(
                    message: "A care facility kind is required.",
                    paramName: nameof(value))
                : value.Trim();

            Value = normalized.Length <= MaxLength
                ? normalized
                : throw new ArgumentOutOfRangeException(
                    paramName: nameof(value),
                    message: $"Care facility kinds cannot exceed {MaxLength} characters.");
        }

        public string Value { get; }

        public override string ToString() => Value;
    }
}
