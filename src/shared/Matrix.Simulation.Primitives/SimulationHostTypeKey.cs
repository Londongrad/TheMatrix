namespace Matrix.Simulation.Primitives;

public readonly record struct SimulationHostTypeKey
{
    public const int MaxLength = SimulationKeyValidator.MaxLength;

    public SimulationHostTypeKey(string value)
    {
        Value = SimulationKeyValidator.Validate(value, nameof(value));
    }

    public string Value { get; }

    public bool IsEmpty => string.IsNullOrEmpty(Value);

    public override string ToString()
    {
        return Value ?? string.Empty;
    }
}
