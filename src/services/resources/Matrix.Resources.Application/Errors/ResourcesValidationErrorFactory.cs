using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;

namespace Matrix.Resources.Application.Errors
{
    public sealed class ResourcesValidationErrorFactory : IValidationExceptionFactory
    {
        public MatrixApplicationException Create(
            Type requestType,
            IReadOnlyDictionary<string, string[]> errors)
        {
            string code = $"Resources.{requestType.Name}.ValidationFailed";

            return new MatrixApplicationException(
                code: code,
                message: "One or more validation errors occurred.",
                errorType: ApplicationErrorType.Validation,
                errors: errors);
        }
    }
}
