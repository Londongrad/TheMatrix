namespace Matrix.Healthcare.Domain.Facilities
{
    public readonly record struct LocationAnchorId
    {
        public LocationAnchorId(Guid value)
        {
            Value = value != Guid.Empty
                ? value
                : throw new ArgumentException(
                    message: "A location anchor identifier is required.",
                    paramName: nameof(value));
        }

        public Guid Value { get; }

        public override string ToString() => Value.ToString();
    }
}
