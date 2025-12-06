using Matrix.BuildingBlocks.Application.Enums;
using Matrix.BuildingBlocks.Application.Exceptions;

namespace Matrix.Identity.Application.Errors
{
    public static class ApplicationErrorsFactory
    {
        public static MatrixApplicationException InvalidCredentials()
            => new(
                code: "Identity.InvalidCredentials",
                message: "Invalid login or password.",
                errorType: ApplicationErrorType.Unauthorized);

        public static MatrixApplicationException UserBlocked()
            => new(
                code: "Identity.UserBlocked",
                message: "User account is blocked and cannot be used to sign in.",
                errorType: ApplicationErrorType.Forbidden);

        public static MatrixApplicationException InvalidRefreshToken()
            => new(
                code: "Identity.InvalidRefreshToken",
                message: "The provided refresh token is invalid or has expired.",
                errorType: ApplicationErrorType.Unauthorized);

        public static MatrixApplicationException EmailAlreadyInUse(string email)
            => new(
                code: "Identity.EmailAlreadyInUse",
                message: $"Email '{email}' is already in use.",
                errorType: ApplicationErrorType.Conflict);

        public static MatrixApplicationException UsernameAlreadyInUse(string username)
            => new(
                code: "Identity.UsernameAlreadyInUse",
                message: $"Username '{username}' is already in use.",
                errorType: ApplicationErrorType.Conflict);

        public static MatrixApplicationException UserNotFound(Guid id)
            => new(
                code: "Identity.User.NotFound",
                message: $"User '{id}' was not found.",
                errorType: ApplicationErrorType.NotFound);

        public static MatrixApplicationException PasswordsDoNotMatch()
            => new(
                code: "Identity.PasswordsDoNotMatch",
                message: "Passwords do not match.",
                errorType: ApplicationErrorType.Validation);

        public static MatrixApplicationException InvalidCurrentPassword()
            => new(
                code: "Identity.InvalidCurrentPassword",
                message: "Current password is incorrect.",
                errorType: ApplicationErrorType.Unauthorized);

        public static MatrixApplicationException ValidationFailed(
            IReadOnlyDictionary<string, string[]> errors)
            => new(
                code: "Identity.ValidationFailed",
                message: "One or more validation errors occurred.",
                errorType: ApplicationErrorType.Validation,
                errors: errors);
    }
}
