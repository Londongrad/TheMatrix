using Matrix.BuildingBlocks.Application.Exceptions;

namespace Matrix.BuildingBlocks.Application.Abstractions
{
    public interface IValidationExceptionFactory
    {
        MatrixApplicationException Create(
            Type requestType,
            IReadOnlyDictionary<string, string[]> errors);
    }
}
