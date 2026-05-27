using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationCore.Domain.Errors;

namespace Matrix.SimulationCore.Domain.Simulation;

public readonly record struct SimulationSeed
{
    public const int MaxLength = 128;

    public SimulationSeed(string? value)
    {
        string normalized = GuardHelper.AgainstNullOrWhiteSpace(
            value: value,
            errorFactory: DomainErrorsFactory.SimulationSeedNullOrEmpty,
            trim: true,
            propertyName: nameof(Value));

        if (normalized.Length > MaxLength)
            throw DomainErrorsFactory.SimulationSeedTooLong(
                value: normalized,
                max: MaxLength,
                propertyName: nameof(Value));

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value ?? string.Empty;
    }
}
