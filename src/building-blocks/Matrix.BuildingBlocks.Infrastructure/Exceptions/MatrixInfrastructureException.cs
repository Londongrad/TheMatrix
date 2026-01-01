using Matrix.BuildingBlocks.Application.Enums;

namespace Matrix.BuildingBlocks.Infrastructure.Exceptions
{
    public sealed class MatrixInfrastructureException(
        string code,
        string message,
        ApplicationErrorType errorType = ApplicationErrorType.Unknown,
        IReadOnlyDictionary<string, string[]>? details = null,
        Exception? innerException = null)
        : Exception(
            message: message,
            innerException: innerException)
    {
        public string Code { get; } = string.IsNullOrWhiteSpace(code)
            ? "Infrastructure.Error"
            : code;

        public ApplicationErrorType ErrorType { get; } = errorType;
        public IReadOnlyDictionary<string, string[]>? Details { get; } = details;
    }
}
