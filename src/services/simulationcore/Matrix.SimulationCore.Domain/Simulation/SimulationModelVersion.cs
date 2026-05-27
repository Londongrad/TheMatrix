using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationCore.Domain.Errors;

namespace Matrix.SimulationCore.Domain.Simulation;

public readonly record struct SimulationModelVersion
{
    public const int MaxLength = 64;

    public SimulationModelVersion(string? value)
    {
        string normalized = GuardHelper.AgainstNullOrWhiteSpace(
            value: value,
            errorFactory: DomainErrorsFactory.SimulationModelVersionNullOrEmpty,
            trim: true,
            propertyName: nameof(Value));

        if (normalized.Length > MaxLength)
            throw DomainErrorsFactory.SimulationModelVersionTooLong(
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
