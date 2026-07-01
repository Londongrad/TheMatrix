namespace Matrix.Healthcare.Domain.Facilities
{
    public readonly record struct CareFacilityId
    {
        public CareFacilityId(Guid value)
        {
            Value = value != Guid.Empty
                ? value
                : throw new ArgumentException(
                    message: "A care facility identifier is required.",
                    paramName: nameof(value));
        }

        public Guid Value { get; }

        public static CareFacilityId New() => new(Guid.NewGuid());

        public override string ToString() => Value.ToString();
    }
}
