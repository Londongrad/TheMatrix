using Matrix.BuildingBlocks.Domain.Exceptions;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Errors
{
    public static class ClassicCityDomainErrorsFactory
    {
        public static DomainException CityPopulationUtcOffsetMinutesOutOfRange(
            int value,
            int min,
            int max,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Environment.UtcOffsetMinutes.OutOfRange",
                message: $"UTC offset minutes must be in range [{min}; {max}].",
                propertyName: propertyName);
        }

        public static DomainException CityPopulationTickIdCannotBeNegative(
            long value,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Progression.TickId.Negative",
                message: "Population progression tick id cannot be negative.",
                propertyName: propertyName);
        }

        public static DomainException CityPopulationTickIdCannotMoveBackwards(
            long value,
            long previous,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Progression.TickId.Backwards",
                message: $"Population progression tick id '{value}' cannot move backwards from '{previous}'.",
                propertyName: propertyName);
        }

        public static DomainException CityPopulationProcessedDateCannotMoveBackwards(
            DateOnly value,
            DateOnly previous,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Progression.ProcessedDate.Backwards",
                message: $"Population progression date '{value}' cannot move backwards from '{previous}'.",
                propertyName: propertyName);
        }

        public static DomainException CityPopulationWeatherImpactSimTimeCannotMoveBackwards(
            DateTimeOffset value,
            DateTimeOffset previous,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.WeatherImpact.SimTime.Backwards",
                message: $"Weather impact sim time '{value:O}' cannot move backwards from '{previous:O}'.",
                propertyName: propertyName);
        }

        public static DomainException CityPopulationWeatherImpactOccurredOnCannotMoveBackwards(
            DateTimeOffset value,
            DateTimeOffset previous,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.WeatherImpact.OccurredOn.Backwards",
                message:
                $"Weather impact occurrence '{value:O}' cannot move backwards from '{previous:O}' at the same sim time.",
                propertyName: propertyName);
        }

        public static DomainException CityPopulationWeatherExposureStaleUpdate(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.WeatherExposure.Update.Stale",
                message: "Cannot apply stale weather exposure update.",
                propertyName: propertyName);
        }

        public static DomainException CityPopulationWeatherExposureProcessedAtCannotMoveBackwards(
            DateTimeOffset value,
            DateTimeOffset previous,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.WeatherExposure.ProcessedAt.Backwards",
                message: $"Weather exposure processed time '{value:O}' cannot move backwards from '{previous:O}'.",
                propertyName: propertyName);
        }

        public static DomainException HousedHouseholdRequiresPlacement(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Household.Placement.HousedRequiresPlacement",
                message: "A housed household must have city, district, and residential building placement.",
                propertyName: propertyName);
        }

        public static DomainException HomelessHouseholdCannotHaveResidentialBuilding(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Household.Placement.HomelessCannotHaveResidentialBuilding",
                message: "A homeless household cannot have a residential building assigned.",
                propertyName: propertyName);
        }

        public static DomainException ResidentialCapacityOutOfRange(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Household.ResidentialCapacity.OutOfRange",
                message: "Residential capacity must be greater than zero.",
                propertyName: propertyName);
        }
    }
}
