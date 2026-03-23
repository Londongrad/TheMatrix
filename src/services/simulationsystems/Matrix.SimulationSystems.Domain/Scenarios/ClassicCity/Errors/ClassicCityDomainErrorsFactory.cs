using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Errors
{
    public static class ClassicCityDomainErrorsFactory
    {
        public static DomainException InvalidCitySystemKind(
            CitySystemKind value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.ClassicCity.System.Kind.Invalid",
                message: $"City system kind '{value}' is invalid.",
                propertyName: propertyName);
        }

        public static DomainException CityNormalizedIndexOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.ClassicCity.Index.OutOfRange",
                message: $"Normalized city system index must be in range [{min}; {max}].",
                propertyName: propertyName);
        }

        public static DomainException CitySystemSnapshotRequired(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.ClassicCity.System.Snapshot.Required",
                message: "City system snapshot is required.",
                propertyName: propertyName);
        }

        public static DomainException CityEnvironmentalConditionSnapshotRequired(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.ClassicCity.Environment.Snapshot.Required",
                message: "City environmental condition snapshot is required.",
                propertyName: propertyName);
        }

        public static DomainException CityEnvironmentalConditionPressureRequired(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.ClassicCity.Environment.Pressure.Required",
                message: "City environmental pressure profile is required.",
                propertyName: propertyName);
        }

        public static DomainException CityWeatherPressureProfileRequired(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.ClassicCity.Environment.WeatherPressure.Required",
                message: "City environmental weather pressure profile is required.",
                propertyName: propertyName);
        }

        public static DomainException CityEnvironmentalConditionSystemSnapshotRequired(
            string systemName,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.ClassicCity.Environment.SystemSnapshot.Required",
                message: $"City environmental condition requires a '{systemName}' system snapshot.",
                propertyName: propertyName);
        }

        public static DomainException CityEnvironmentalTimestampMustBeUtc(
            DateTimeOffset value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.ClassicCity.Environment.Timestamp.NotUtc",
                message: "Classic City environmental timestamps must be in UTC (Offset=00:00).",
                propertyName: propertyName);
        }

        public static DomainException CityEnvironmentalConditionSnapshotCannotMoveBackwards(
            DateTimeOffset value,
            DateTimeOffset previous,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.ClassicCity.Environment.Snapshot.Backwards",
                message: $"Environmental condition snapshot '{value:O}' cannot move backwards from '{previous:O}'.",
                propertyName: propertyName);
        }

        public static DomainException CityEnvironmentalConditionAdvanceWindowInvalid(
            DateTimeOffset from,
            DateTimeOffset to,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.ClassicCity.Environment.AdvanceWindow.Invalid",
                message: $"Environmental advance window '{from:O}' -> '{to:O}' must move forward.",
                propertyName: propertyName);
        }

        public static DomainException CitySystemSnapshotKindMismatch(
            CitySystemKind value,
            CitySystemKind expected,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationSystems.ClassicCity.System.Snapshot.KindMismatch",
                message: $"City system snapshot kind '{value}' cannot be applied to '{expected}'.",
                propertyName: propertyName);
        }
    }
}
