using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;

namespace Matrix.Population.Application.Errors
{
    public static class ApplicationErrorsFactory
    {
        public static MatrixApplicationException PersonNotFound(Guid id)
        {
            return new MatrixApplicationException(
                code: "Population.Person.NotFound",
                message: $"Person '{id}' was not found.",
                errorType: ApplicationErrorType.NotFound);
        }
    }
}
