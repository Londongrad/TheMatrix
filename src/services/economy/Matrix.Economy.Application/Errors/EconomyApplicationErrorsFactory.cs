using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;

namespace Matrix.Economy.Application.Errors
{
    public static class EconomyApplicationErrorsFactory
    {
        public static MatrixApplicationException CannotInitializeDeletedCity(Guid cityId)
        {
            return new MatrixApplicationException(
                code: "Economy.City.Deleted",
                message: $"Cannot initialize economy for deleted city '{cityId}'.",
                errorType: ApplicationErrorType.Conflict);
        }
    }
}
