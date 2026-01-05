using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;

namespace Matrix.Identity.Application.Errors
{
    public static class ApplicationErrorsFactory
    {
        public static MatrixApplicationException InvalidCredentials()
        {
            return new MatrixApplicationException(
                code: "Identity.InvalidCredentials",
                message: "Invalid login or password.",
                errorType: ApplicationErrorType.Unauthorized);
        }

        public static MatrixApplicationException UserBlocked()
        {
            return new MatrixApplicationException(
                code: "Identity.UserBlocked",
                message: "User account is blocked and cannot be used to sign in.",
                errorType: ApplicationErrorType.Forbidden);
        }

        public static MatrixApplicationException InvalidRefreshToken()
        {
            return new MatrixApplicationException(
                code: "Identity.InvalidRefreshToken",
                message: "The provided refresh token is invalid or has expired.",
                errorType: ApplicationErrorType.Unauthorized);
        }

        public static MatrixApplicationException EmailAlreadyInUse(string email)
        {
            return new MatrixApplicationException(
                code: "Identity.EmailAlreadyInUse",
                message: $"Email '{email}' is already in use.",
                errorType: ApplicationErrorType.Conflict);
        }

        public static MatrixApplicationException UsernameAlreadyInUse(string username)
        {
            return new MatrixApplicationException(
                code: "Identity.UsernameAlreadyInUse",
                message: $"Username '{username}' is already in use.",
                errorType: ApplicationErrorType.Conflict);
        }

        public static MatrixApplicationException UserNotFound(Guid id)
        {
            return new MatrixApplicationException(
                code: "Identity.User.NotFound",
                message: $"User '{id}' was not found.",
                errorType: ApplicationErrorType.NotFound);
        }

        public static MatrixApplicationException RoleNotFound(Guid id)
        {
            return new MatrixApplicationException(
                code: "Identity.Role.NotFound",
                message: $"Role '{id}' was not found.",
                errorType: ApplicationErrorType.NotFound);
        }

        public static MatrixApplicationException RoleNameAlreadyInUse(string name)
        {
            return new MatrixApplicationException(
                code: "Identity.Role.Name.AlreadyInUse",
                message: $"Role name '{name}' is already in use.",
                errorType: ApplicationErrorType.Conflict);
        }

        public static MatrixApplicationException PasswordsDoNotMatch()
        {
            return new MatrixApplicationException(
                code: "Identity.PasswordsDoNotMatch",
                message: "Passwords do not match.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException InvalidCurrentPassword()
        {
            return new MatrixApplicationException(
                code: "Identity.InvalidCurrentPassword",
                message: "Current password is incorrect.",
                errorType: ApplicationErrorType.Unauthorized);
        }

        public static MatrixApplicationException PermissionNotFound(string key)
        {
            return new MatrixApplicationException(
                code: "Identity.Permission.NotFound",
                message: $"Permission '{key}' was not found.",
                errorType: ApplicationErrorType.NotFound);
        }

        public static MatrixApplicationException PermissionDeprecated(string key)
        {
            return new MatrixApplicationException(
                code: "Identity.Permission.Deprecated",
                message: $"Permission '{key}' is deprecated.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException ValidationFailed(IReadOnlyDictionary<string, string[]> errors)
        {
            return new MatrixApplicationException(
                code: "Identity.ValidationFailed",
                message: "One or more validation errors occurred.",
                errorType: ApplicationErrorType.Validation,
                errors: errors);
        }

        public static MatrixApplicationException EmptyId(string name = "Id")
        {
            return new MatrixApplicationException(
                code: "Identity.Id.Empty",
                message: $"{name} must not be empty.",
                errorType: ApplicationErrorType.Validation);
        }
    }
}
