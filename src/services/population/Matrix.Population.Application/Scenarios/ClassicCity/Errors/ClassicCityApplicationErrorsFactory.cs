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
    }
}
