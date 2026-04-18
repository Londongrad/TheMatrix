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

        public static MatrixApplicationException TooManyAuthenticationAttempts()
        {
            return new MatrixApplicationException(
                code: "Identity.Auth.TooManyAttempts",
                message: "Too many authentication attempts. Please try again in a few minutes.",
                errorType: ApplicationErrorType.TooManyRequests);
        }

        public static MatrixApplicationException UserBlocked()
        {
            return new MatrixApplicationException(
                code: "Identity.UserBlocked",
                message: "User account is blocked and cannot be used to sign in.",
                errorType: ApplicationErrorType.Forbidden);
        }

        public static MatrixApplicationException AccountDeleted()
        {
            return new MatrixApplicationException(
                code: "Identity.AccountDeleted",
                message: "This account was deleted and cannot be used until it is restored.",
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

        public static MatrixApplicationException PendingEmailAlreadyInUse(string email)
        {
            return new MatrixApplicationException(
                code: "Identity.PendingEmailAlreadyInUse",
                message: $"Email '{email}' is already reserved by another pending confirmation flow.",
                errorType: ApplicationErrorType.Conflict);
        }

        public static MatrixApplicationException EmailChangeRequiresDifferentAddress()
        {
            return new MatrixApplicationException(
                code: "Identity.EmailChange.RequiresDifferentAddress",
                message: "New email must be different from the current email.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException EmailChangePendingEmailMissing()
        {
            return new MatrixApplicationException(
                code: "Identity.EmailChange.PendingEmailMissing",
                message: "There is no pending email change to confirm.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException EmailChangePendingRequestMissing()
        {
            return new MatrixApplicationException(
                code: "Identity.EmailChange.PendingRequestMissing",
                message: "There is no pending email change for this account.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException EmailChangeRequestThrottled()
        {
            return new MatrixApplicationException(
                code: "Identity.EmailChange.RequestThrottled",
                message: "Too many email change requests. Please try again in a few minutes.",
                errorType: ApplicationErrorType.TooManyRequests);
        }

        public static MatrixApplicationException UsernameAlreadyInUse(string username)
        {
            return new MatrixApplicationException(
                code: "Identity.UsernameAlreadyInUse",
                message: $"Username '{username}' is already in use.",
                errorType: ApplicationErrorType.Conflict);
        }

        public static MatrixApplicationException UsernameChangeCooldown(DateTime nextAllowedAtUtc)
        {
            return new MatrixApplicationException(
                code: "Identity.UsernameChangeCooldown",
                message: $"Username can be changed again after {nextAllowedAtUtc:yyyy-MM-dd HH:mm} UTC.",
                errorType: ApplicationErrorType.Validation);
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

        public static MatrixApplicationException SystemRoleIsReadOnly(string roleName)
        {
            return new MatrixApplicationException(
                code: "Identity.Role.System.ReadOnly",
                message: $"System role '{roleName}' cannot be renamed, deleted, or edited.",
                errorType: ApplicationErrorType.Forbidden);
        }

        public static MatrixApplicationException CannotPerformAdminActionOnSelf()
        {
            return new MatrixApplicationException(
                code: "Identity.Admin.SelfActionForbidden",
                message: "You cannot perform this admin action on yourself.",
                errorType: ApplicationErrorType.Forbidden);
        }

        public static MatrixApplicationException SuperAdminUserIsProtected()
        {
            return new MatrixApplicationException(
                code: "Identity.Admin.SuperAdmin.Protected",
                message: "SuperAdmin user access is protected and cannot be changed through admin actions.",
                errorType: ApplicationErrorType.Forbidden);
        }

        public static MatrixApplicationException SuperAdminRoleAssignmentForbidden()
        {
            return new MatrixApplicationException(
                code: "Identity.Admin.SuperAdmin.RoleAssignmentForbidden",
                message: "SuperAdmin role cannot be assigned or removed through generic admin role editing.",
                errorType: ApplicationErrorType.Forbidden);
        }

        public static MatrixApplicationException RequiredSystemRoleMissing(string roleName)
        {
            return new MatrixApplicationException(
                code: "Identity.Role.System.Missing",
                message: $"Required system role '{roleName}' is not configured.",
                errorType: ApplicationErrorType.BusinessRule);
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

        public static MatrixApplicationException AvatarFormatNotSupported()
        {
            return new MatrixApplicationException(
                code: "Identity.Avatar.UnsupportedFormat",
                message: "Avatar must be a JPG, PNG, or WebP image.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException AvatarContentInvalid()
        {
            return new MatrixApplicationException(
                code: "Identity.Avatar.InvalidContent",
                message: "Avatar file is not a valid image.",
                errorType: ApplicationErrorType.Validation);
        }

        public static MatrixApplicationException AccountDeletionRequiresPassword()
        {
            return new MatrixApplicationException(
                code: "Identity.AccountDeletionRequiresPassword",
                message: "Current password is required to delete the account.",
                errorType: ApplicationErrorType.Validation);
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
