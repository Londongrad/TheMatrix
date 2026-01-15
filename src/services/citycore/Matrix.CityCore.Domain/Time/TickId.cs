namespace Matrix.CityCore.Domain.Time
{
    /// <summary>
    ///     Monotonic tick identifier used for idempotency in downstream consumers.
    /// </summary>
    public readonly record struct TickId(long Value)
    {
        public static TickId Start()
        {
            return new TickId(0);
        }

        public TickId Next()
        {
            return new TickId(checked(Value + 1));
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
