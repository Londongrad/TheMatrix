using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Errors
{
    public static class ClassicCityDomainErrorsFactory
    {
        public static DomainException CityNameNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.Name.NullOrEmpty",
                message: "City name cannot be null or empty.",
                propertyName: propertyName);
        }

        public static DomainException CityNameTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.Name.TooLong",
                message: $"City name cannot be longer than {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException CityTimestampMustBeUtc(
            DateTimeOffset value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.Timestamp.NotUtc",
                message: "City timestamps must be in UTC (Offset=00:00).",
                propertyName: propertyName);
        }

        public static DomainException CityIsArchived(
            CityStatus value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.Archived",
                message: "Operation is not allowed for an archived city.",
                propertyName: propertyName);
        }

        public static DomainException InvalidCityEnvironment(
            string reason,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.Environment.Invalid",
                message: $"City environment is invalid. {reason}",
                propertyName: propertyName);
        }

        public static DomainException CityUtcOffsetOutOfRange(
            int valueMinutes,
            int minMinutes,
            int maxMinutes,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.UtcOffset.OutOfRange",
                message: $"City UTC offset must be in range [{minMinutes}; {maxMinutes}] minutes.",
                propertyName: propertyName);
        }

        public static DomainException CityUtcOffsetMustAlignToStep(
            int valueMinutes,
            int stepMinutes,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.UtcOffset.InvalidStep",
                message: $"City UTC offset must align to {stepMinutes}-minute increments.",
                propertyName: propertyName);
        }

        public static DomainException CityGenerationSeedNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.GenerationSeed.NullOrEmpty",
                message: "City generation seed cannot be null or empty.",
                propertyName: propertyName);
        }

        public static DomainException CityGenerationSeedTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.GenerationSeed.TooLong",
                message: $"City generation seed cannot be longer than {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException ScenarioModelSetVersionNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.ScenarioModelSetVersion.NullOrEmpty",
                message: "Scenario model-set version cannot be null or empty.",
                propertyName: propertyName);
        }

        public static DomainException ScenarioModelSetVersionTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.ScenarioModelSetVersion.TooLong",
                message: $"Scenario model-set version cannot be longer than {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException InvalidCityGenerationProfile(
            string reason,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.GenerationProfile.Invalid",
                message: $"City generation profile is invalid. {reason}",
                propertyName: propertyName);
        }

        public static DomainException CityPopulationBootstrapFailureCodeNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.PopulationBootstrap.FailureCode.NullOrEmpty",
                message: "Population bootstrap failure code cannot be null or empty.",
                propertyName: propertyName);
        }

        public static DomainException CityPopulationBootstrapFailureCodeTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.PopulationBootstrap.FailureCode.TooLong",
                message: $"Population bootstrap failure code cannot be longer than {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException CityPopulationBootstrapFailureCodeInvalid(
            string value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.City.PopulationBootstrap.FailureCode.Invalid",
                message:
                $"Population bootstrap failure code '{value}' must contain only ASCII letters, digits, or underscores.",
                propertyName: propertyName);
        }

        public static DomainException TopologyTimestampMustBeUtc(
            DateTimeOffset value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.Timestamp.NotUtc",
                message: "Topology timestamps must be in UTC (Offset=00:00).",
                propertyName: propertyName);
        }

        public static DomainException DistrictNameNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.District.Name.NullOrEmpty",
                message: "District name cannot be null or empty.",
                propertyName: propertyName);
        }

        public static DomainException DistrictNameTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.District.Name.TooLong",
                message: $"District name cannot be longer than {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException ResidentialBuildingNameNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.ResidentialBuilding.Name.NullOrEmpty",
                message: "Residential building name cannot be null or empty.",
                propertyName: propertyName);
        }

        public static DomainException ResidentialBuildingNameTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.ResidentialBuilding.Name.TooLong",
                message: $"Residential building name cannot be longer than {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException TopologyCoordinateOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.Coordinate.OutOfRange",
                message: $"Topology coordinate must be in range [{min}; {max}].",
                propertyName: propertyName);
        }

        public static DomainException RoadNodeNameNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.RoadNode.Name.NullOrEmpty",
                message: "Road node name cannot be null or empty.",
                propertyName: propertyName);
        }

        public static DomainException RoadNodeNameTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.RoadNode.Name.TooLong",
                message: $"Road node name cannot be longer than {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException RoadSegmentNameNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.RoadSegment.Name.NullOrEmpty",
                message: "Road segment name cannot be null or empty.",
                propertyName: propertyName);
        }

        public static DomainException RoadSegmentNameTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.RoadSegment.Name.TooLong",
                message: $"Road segment name cannot be longer than {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException RoadSegmentLengthOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.RoadSegment.Length.OutOfRange",
                message: $"Road segment length must be in range [{min}; {max}] meters.",
                propertyName: propertyName);
        }

        public static DomainException RoadSegmentEndpointsMustDiffer(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.RoadSegment.Endpoints.Invalid",
                message: "Road segment endpoints must reference two different road nodes.",
                propertyName: propertyName);
        }

        public static DomainException CityAnchorNameNullOrEmpty(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.CityAnchor.Name.NullOrEmpty",
                message: "City anchor name cannot be null or empty.",
                propertyName: propertyName);
        }

        public static DomainException CityAnchorNameTooLong(
            string value,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.CityAnchor.Name.TooLong",
                message: $"City anchor name cannot be longer than {max} characters.",
                propertyName: propertyName);
        }

        public static DomainException CityAnchorCapacityOutOfRange(
            int value,
            int min,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.CityAnchor.Capacity.OutOfRange",
                message: $"City anchor capacity must be in range [{min}; {max}].",
                propertyName: propertyName);
        }

        public static DomainException ResidentCapacityOutOfRange(
            int value,
            int min,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Topology.ResidentialBuilding.Capacity.OutOfRange",
                message: $"Resident capacity must be in range [{min}; {max}].",
                propertyName: propertyName);
        }

        public static DomainException TemperatureCOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.Temperature.OutOfRange",
                message: $"Temperature must be in range [{min}; {max}] degrees Celsius.",
                propertyName: propertyName);
        }

        public static DomainException HumidityPercentOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.Humidity.OutOfRange",
                message: "Humidity must be in range [0; 100] percent.",
                propertyName: propertyName);
        }

        public static DomainException WindSpeedKphOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.WindSpeed.OutOfRange",
                message: $"Wind speed must be in range [{min}; {max}] kph.",
                propertyName: propertyName);
        }

        public static DomainException CloudCoveragePercentOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.CloudCoverage.OutOfRange",
                message: "Cloud coverage must be in range [0; 100] percent.",
                propertyName: propertyName);
        }

        public static DomainException PressureHpaOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.Pressure.OutOfRange",
                message: $"Pressure must be in range [{min}; {max}] hPa.",
                propertyName: propertyName);
        }

        public static DomainException WeatherVolatilityOutOfRange(
            decimal value,
            decimal min,
            decimal max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.Volatility.OutOfRange",
                message: "Weather volatility must be in range [0; 1].",
                propertyName: propertyName);
        }

        public static DomainException InvalidWeatherStateTimeRange(
            SimTime startedAt,
            SimTime expectedUntil,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.State.TimeRange.Invalid",
                message: $"Weather state ExpectedUntil ({expectedUntil}) must be greater than StartedAt ({startedAt}).",
                propertyName: propertyName);
        }

        public static DomainException InvalidOverrideTimeRange(
            SimTime startsAt,
            SimTime endsAt,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.Override.TimeRange.Invalid",
                message: $"Weather override EndsAt ({endsAt}) must be greater than StartsAt ({startsAt}).",
                propertyName: propertyName);
        }

        public static DomainException OverrideAlreadyActive(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.Override.AlreadyActive",
                message: "Only one active weather override is allowed per city.",
                propertyName: propertyName);
        }

        public static DomainException NoActiveOverrideToCancel(string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.Override.NotActive",
                message: "There is no active weather override to cancel or expire.",
                propertyName: propertyName);
        }

        public static DomainException WeatherEvaluationTimeGoingBackwards(
            SimTime value,
            SimTime previous,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.Evaluation.Time.Backwards",
                message:
                $"Weather evaluation time ({value}) cannot be earlier than the last evaluated time ({previous}).",
                propertyName: propertyName);
        }

        public static DomainException InvalidClimateProfile(
            string reason,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.ClimateProfile.Invalid",
                message: $"Weather climate profile is invalid. {reason}",
                propertyName: propertyName);
        }

        public static DomainException InvalidWeatherTransitionTiming(
            SimTime evaluatedAt,
            SimTime startedAt,
            SimTime expectedUntil,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.Transition.Timing.Invalid",
                message:
                $"Weather state must be active at evaluation time ({evaluatedAt}); active range is [{startedAt}; {expectedUntil}).",
                propertyName: propertyName);
        }

        public static DomainException IncoherentWeatherPrecipitation(
            WeatherType type,
            PrecipitationKind precipitationKind,
            string? propertyName = null)
        {
            return new DomainException(
                code: "SimulationCore.Weather.Precipitation.Incoherent",
                message: $"Precipitation kind '{precipitationKind}' is not coherent with weather type '{type}'.",
                propertyName: propertyName);
        }
    }
}
