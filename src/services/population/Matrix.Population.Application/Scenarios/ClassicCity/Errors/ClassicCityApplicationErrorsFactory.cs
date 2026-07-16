using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;

namespace Matrix.Population.Application.Scenarios.ClassicCity.Errors
{
    public static class ClassicCityApplicationErrorsFactory
    {
        public static MatrixApplicationException CannotInitializePopulationForArchivedCity(Guid cityId)
        {
            return new MatrixApplicationException(
                code: "Population.City.Archived",
                message: $"Cannot initialize population for archived city '{cityId}'.",
                errorType: ApplicationErrorType.Conflict);
        }

        public static MatrixApplicationException CannotInitializePopulationForDeletedCity(Guid cityId)
        {
            return new MatrixApplicationException(
                code: "Population.City.Deleted",
                message: $"Cannot initialize population for deleted city '{cityId}'.",
                errorType: ApplicationErrorType.Conflict);
        }

        public static MatrixApplicationException HouseholdPlacementNotFound(
            Guid householdId,
            Guid cityId)
        {
            return new MatrixApplicationException(
                code: "Population.ClassicCity.HouseholdPlacement.NotFound",
                message: $"Household '{householdId}' has no classic city placement for city '{cityId}'.",
                errorType: ApplicationErrorType.NotFound);
        }

        public static MatrixApplicationException PersonNotAssignedToCity(
            Guid personId,
            Guid cityId)
        {
            return new MatrixApplicationException(
                code: "Population.Person.NotAssignedToCity",
                message: $"Person '{personId}' is not assigned to city '{cityId}'.",
                errorType: ApplicationErrorType.BusinessRule);
        }

        public static MatrixApplicationException EmploymentWorkplaceNotFound(
            Guid workplaceId,
            Guid cityId)
        {
            return new MatrixApplicationException(
                code: "Population.Employment.Workplace.NotFound",
                message: $"Workplace '{workplaceId}' was not found inside city '{cityId}'.",
                errorType: ApplicationErrorType.NotFound);
        }

    }
}
