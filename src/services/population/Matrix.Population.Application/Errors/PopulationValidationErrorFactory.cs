using Matrix.BuildingBlocks.Application.Abstractions;
using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;

namespace Matrix.Population.Application.Errors
{
    public sealed class PopulationValidationErrorFactory : IValidationExceptionFactory
    {
        public MatrixApplicationException Create(
            Type requestType,
            IReadOnlyDictionary<string, string[]> errors)
        {
            var code = $"Population.{requestType.Name}.ValidationFailed";
            
            return new MatrixApplicationException(
                code,
                "One or more validation errors occurred.",
                ApplicationErrorType.Validation,
                errors);
        }
    }
}
