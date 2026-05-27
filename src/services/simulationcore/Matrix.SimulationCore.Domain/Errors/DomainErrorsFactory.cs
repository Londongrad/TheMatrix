using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Errors
{
    public static class DomainErrorsFactory
    {
        #region [ Simulation ]

        public static DomainException SimTimeMustBeUtc(
            DateTimeOffset value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.SimTime.NotUtc",
                message: "SimTime must be in UTC (Offset=00:00).",
                propertyName: propertyName);
        }

        public static DomainException SimSpeedMultiplierOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.SimSpeed.Multiplier.OutOfRange",
                message: $"SimSpeed multiplier must be in range [{min}; {max}].",
                propertyName: propertyName);
        }

        public static DomainException SimSpeedRealDeltaMustBePositive(
            TimeSpan value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.SimSpeed.RealDelta.NotPositive",
                message: "realDelta must be positive.",
                propertyName: propertyName);
        }

        public static DomainException SimulationSeedNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Simulation.Seed.NullOrEmpty",
                message: "Simulation seed cannot be empty.",
                propertyName: propertyName);
        }

        public static DomainException SimulationSeedTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Simulation.Seed.TooLong",
                message: $"Simulation seed cannot exceed {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException SimulationModelVersionNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Simulation.ModelVersion.NullOrEmpty",
                message: "Simulation model version cannot be empty.",
                propertyName: propertyName);
        }

        public static DomainException SimulationModelVersionTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Simulation.ModelVersion.TooLong",
                message: $"Simulation model version cannot exceed {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException SimulationRuntimeKeyMissing(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Simulation.RuntimeKey.Missing",
                message: "Simulation runtime key is required.",
                propertyName: propertyName);
        }

        public static DomainException SimulationTimestampMustBeUtc(
            DateTimeOffset value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Simulation.Timestamp.NotUtc",
                message: "Simulation timestamps must use UTC.",
                propertyName: propertyName);
        }

        public static DomainException SimulationInitialStateInvalid(
            SimulationHostState value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Simulation.InitialState.Invalid",
                message: $"Simulation cannot be created in state '{value}'.",
                propertyName: propertyName);
        }

        #endregion [ Simulation ]
    }
}
