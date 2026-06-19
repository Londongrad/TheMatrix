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

        public static MatrixApplicationException CivilRegistryResidentsAreNotCurrentSpouses(
            Guid firstResidentId,
            Guid secondResidentId)
        {
            return new MatrixApplicationException(
                code: "Population.CivilRegistry.ResidentsAreNotCurrentSpouses",
                message:
                $"Residents '{firstResidentId}' and '{secondResidentId}' are not currently married to each other.",
                errorType: ApplicationErrorType.BusinessRule);
        }

        public static MatrixApplicationException HouseholdNotFound(Guid householdId)
        {
            return new MatrixApplicationException(
                code: "Population.Household.NotFound",
                message: $"Household '{householdId}' was not found.",
                errorType: ApplicationErrorType.NotFound);
        }

        public static MatrixApplicationException EducationInstitutionLevelMismatch(
            Guid institutionId,
            string expectedEducationLevel,
            string actualEducationLevel)
        {
            return new MatrixApplicationException(
                code: "Population.Education.Institution.LevelMismatch",
                message:
                $"Education institution '{institutionId}' is registered for level '{actualEducationLevel}', but '{expectedEducationLevel}' is required.",
                errorType: ApplicationErrorType.BusinessRule);
        }

        public static MatrixApplicationException InvalidEducationLevel(string? value)
        {
            string normalizedValue = string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Trim();

            return new MatrixApplicationException(
                code: "Population.Education.Level.Invalid",
                message: $"Education level '{normalizedValue}' is not supported.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException ResidentAlreadyStudent(Guid residentId)
        {
            return new MatrixApplicationException(
                code: "Population.Education.ResidentAlreadyStudent",
                message: $"Resident '{residentId}' is already marked as a student.",
                errorType: ApplicationErrorType.BusinessRule);
        }

        public static MatrixApplicationException ResidentMustBeStudent(
            Guid residentId,
            string action)
        {
            return new MatrixApplicationException(
                code: "Population.Education.ResidentMustBeStudent",
                message: $"Resident '{residentId}' must currently be a student to {action}.",
                errorType: ApplicationErrorType.BusinessRule);
        }

        public static MatrixApplicationException RetiredResidentCannotStudy(Guid residentId)
        {
            return new MatrixApplicationException(
                code: "Population.Education.RetiredResidentCannotStudy",
                message: $"Resident '{residentId}' cannot re-enter study after retirement through this service.",
                errorType: ApplicationErrorType.BusinessRule);
        }

        public static MatrixApplicationException SeniorResidentCannotStudy(Guid residentId)
        {
            return new MatrixApplicationException(
                code: "Population.Education.SeniorResidentCannotStudy",
                message:
                $"Resident '{residentId}' is already in a senior age group and cannot start studying through this service.",
                errorType: ApplicationErrorType.BusinessRule);
        }

        public static MatrixApplicationException DeceasedResidentCannotStudy(
            Guid residentId,
            string action)
        {
            return new MatrixApplicationException(
                code: "Population.Education.DeceasedResidentCannotStudy",
                message: $"Resident '{residentId}' must be alive to {action}.",
                errorType: ApplicationErrorType.BusinessRule);
        }

        public static MatrixApplicationException ResidentAlreadyAtEducationLevel(
            Guid residentId,
            string educationLevel)
        {
            return new MatrixApplicationException(
                code: "Population.Education.ResidentAlreadyAtLevel",
                message: $"Resident '{residentId}' is already at education level '{educationLevel}'.",
                errorType: ApplicationErrorType.BusinessRule);
        }
    }
}
