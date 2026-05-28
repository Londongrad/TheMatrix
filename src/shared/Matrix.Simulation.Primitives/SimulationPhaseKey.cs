namespace Matrix.Simulation.Primitives;

public readonly record struct SimulationPhaseKey
{
    public const int MaxLength = SimulationKeyValidator.MaxLength;

    public SimulationPhaseKey(string value)
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
