using Matrix.BuildingBlocks.Domain;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Errors;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities
{
    /// <summary>
    ///     Explicit version marker for the scenario/model bundle that produced a simulation run.
    /// </summary>
    public readonly record struct ScenarioModelSetVersion
    {
        public const int MaxLength = 64;
        public const string DefaultValue = "classic-city-v1";

        public ScenarioModelSetVersion(string? value)
        {
            string normalized = GuardHelper.AgainstNullOrWhiteSpace(
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.ScenarioModelSetVersionNullOrEmpty,
                trim: true,
                propertyName: nameof(Value));

            if (normalized.Length > MaxLength)
                throw ClassicCityDomainErrorsFactory.ScenarioModelSetVersionTooLong(
                    value: normalized,
                    max: MaxLength,
                    propertyName: nameof(Value));

            Value = normalized;
        }

        public string Value { get; }

        public static ScenarioModelSetVersion Default()
        {
            return new ScenarioModelSetVersion(DefaultValue);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
