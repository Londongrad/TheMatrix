using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;

namespace Matrix.Population.Application.Errors
{
    public static class ApplicationErrorsFactory
    {
        public static MatrixApplicationException Required(string? propertyName = null)
        {
            string normalizedPropertyName = string.IsNullOrWhiteSpace(propertyName)
                ? "Value"
                : propertyName;

            return new MatrixApplicationException(
                code: "Population.Argument.Required",
                message: $"{normalizedPropertyName} is required.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException EmptyId(string? propertyName = null)
        {
            string normalizedPropertyName = string.IsNullOrWhiteSpace(propertyName)
                ? "Id"
                : propertyName;

            return new MatrixApplicationException(
                code: "Population.Argument.EmptyId",
                message: $"{normalizedPropertyName} must not be empty.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException TimestampMustBeUtc(
            DateTimeOffset value,
            string? propertyName = null)
        {
            string normalizedPropertyName = string.IsNullOrWhiteSpace(propertyName)
                ? "Timestamp"
                : propertyName;

            return new MatrixApplicationException(
                code: "Population.Argument.Timestamp.NotUtc",
                message: $"{normalizedPropertyName} must be in UTC (Offset=00:00).",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException TimestampMustBeUtc(
            DateTime value,
            string? propertyName = null)
        {
            string normalizedPropertyName = string.IsNullOrWhiteSpace(propertyName)
                ? "Timestamp"
                : propertyName;

            return new MatrixApplicationException(
                code: "Population.Argument.Timestamp.NotUtc",
                message: $"{normalizedPropertyName} must be in UTC.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException NumberMustNotBeNegative(
            long value,
            string? propertyName = null)
        {
            string normalizedPropertyName = string.IsNullOrWhiteSpace(propertyName)
                ? "Value"
                : propertyName;

            return new MatrixApplicationException(
                code: "Population.Argument.Negative",
                message: $"{normalizedPropertyName} must not be negative.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException InvalidDateRange(
            DateOnly from,
            DateOnly to,
            string fromName,
            string toName)
        {
            return new MatrixApplicationException(
                code: "Population.Argument.DateRange.Invalid",
                message: $"{toName} '{to:yyyy-MM-dd}' cannot be earlier than {fromName} '{from:yyyy-MM-dd}'.",
                errorType: ApplicationErrorType.Validation);
        }

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

        public static MatrixApplicationException InvalidGenerationContent(
            string catalogName,
            string reason)
        {
            return new MatrixApplicationException(
                code: "Population.Generation.Content.Invalid",
                message: $"Population generation catalog '{catalogName}' is invalid. {reason}",
                errorType: ApplicationErrorType.BusinessRule);
        }

        public static MatrixApplicationException PersonNotFound(Guid id)
        {
            return new MatrixApplicationException(
                code: "Population.Person.NotFound",
                message: $"Person '{id}' was not found.",
                errorType: ApplicationErrorType.NotFound);
        }
    }
}
