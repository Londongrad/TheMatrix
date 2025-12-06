using Matrix.BuildingBlocks.Application.Enums;

namespace Matrix.BuildingBlocks.Application.Exceptions
{
    public sealed class MatrixApplicationException(
        string code,
        string message,
        ApplicationErrorType errorType = ApplicationErrorType.BusinessRule,
        IReadOnlyDictionary<string, string[]>? errors = null)
        : Exception(message)
    {
        public ApplicationErrorType ErrorType { get; } = errorType;

        public string Code { get; } = string.IsNullOrWhiteSpace(code)
            ? "Application.Error"
            : code;

        public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;

        public override string ToString()
        {
            string codePart = $"[{Code}]";

            return $"[{GetType().Name}]: [{ErrorType}] [{codePart}] [{Message}{Environment.NewLine}{StackTrace}]";
        }
    }
}
