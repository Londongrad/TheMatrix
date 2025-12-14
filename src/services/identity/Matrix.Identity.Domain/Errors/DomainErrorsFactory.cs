using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Enums;

namespace Matrix.Identity.Domain.Errors
{
    public static class DomainErrorsFactory
    {
        #region [ Common ]

        public static DomainException EmptyId(string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.Common.EmptyId",
                message: "Id cannot be empty.",
                propertyName: propertyName);
        }

        #endregion [ Common ]

        #region [ User - Credentials ]

        public static DomainException EmptyEmail(string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.User.Email.Empty",
                message: "Email is required.",
                propertyName: propertyName);
        }

        public static DomainException InvalidEmailFormat(string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.User.Email.InvalidFormat",
                message: "Email format is invalid.",
                propertyName: propertyName);
        }

        public static DomainException EmptyUsername(string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.User.Username.Empty",
                message: "Username is required.",
                propertyName: propertyName);
        }

        public static DomainException InvalidUsernameLength(
            int minLength,
            int maxLength,
            int actualLength,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.User.Username.InvalidLength",
                message:
                $"Username must be between {minLength} and {maxLength} characters. Actual length {actualLength}.",
                propertyName: propertyName);
        }

        public static DomainException EmptyPasswordHash(string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.User.Password.EmptyHash",
                message: "Password hash is required.",
                propertyName: propertyName);
        }

        #endregion [ User - Credentials ]

        #region [ User - RefreshToken ]

        public static DomainException InvalidExpireDate(string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.User.RefreshToken.InvalidExpireDate",
                message: "Refresh token expiration must be in the future.",
                propertyName: propertyName);
        }

        public static DomainException RefreshTokenNotFound(string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.User.RefreshToken.NotFound",
                message: "Refresh token not found.",
                propertyName: propertyName);
        }

        #endregion [ User - RefreshToken ]

        #region [ OneTimeToken ]

        public static DomainException EmptyOneTimeTokenHash(string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.OneTimeToken.EmptyTokenHash",
                message: "Token is required.",
                propertyName: propertyName);
        }

        public static DomainException InvalidOneTimeTokenPurpose(
            OneTimeTokenPurpose actualValue,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.OneTimeToken.InvalidPurpose",
                message: $"Token purpose is invalid. Actual value: {actualValue}.",
                propertyName: propertyName);
        }

        public static DomainException OneTimeTokenNotFound(string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.OneTimeToken.NotFound",
                message: "Token not found.",
                propertyName: propertyName);
        }

        public static DomainException InvalidOneTimeTokenExpiration(
            DateTime createdAtUtc,
            DateTime expiresAtUtc,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.OneTimeToken.InvalidExpiration",
                message:
                $"Token expiration must be greater than created time. CreatedAtUtc: {createdAtUtc:o}, ExpiresAtUtc: {expiresAtUtc:o}.",
                propertyName: propertyName);
        }

        public static DomainException OneTimeTokenExpired(
            DateTime expiresAtUtc,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.OneTimeToken.Expired",
                message: $"Token is expired. ExpiresAtUtc: {expiresAtUtc:o}.",
                propertyName: propertyName);
        }

        public static DomainException OneTimeTokenAlreadyUsed(
            DateTime usedAtUtc,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.OneTimeToken.AlreadyUsed",
                message: $"Token is already used. UsedAtUtc: {usedAtUtc:o}.",
                propertyName: propertyName);
        }

        public static DomainException OneTimeTokenRevoked(
            DateTime revokedAtUtc,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.OneTimeToken.Revoked",
                message: $"Token is revoked. RevokedAtUtc: {revokedAtUtc:o}.",
                propertyName: propertyName);
        }

        public static DomainException OneTimeTokenPurposeMismatch(
            OneTimeTokenPurpose expected,
            OneTimeTokenPurpose actual,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.OneTimeToken.PurposeMismatch",
                message: $"Token purpose mismatch. Expected: {expected}, Actual: {actual}.",
                propertyName: propertyName);
        }

        #endregion [ OneTimeToken ]

        #region [ DeviceInfo ]

        public static DomainException InvalidDeviceId(string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.DeviceInfo.InvalidDeviceId",
                message: $"{propertyName} is null or empty.",
                propertyName: propertyName);
        }

        public static DomainException InvalidDeviceName(string? propertyName = null)
        {
            return new DomainException(
                code: "Identity.DeviceInfo.InvalidDeviceName",
                message: $"{propertyName} is null or empty.",
                propertyName: propertyName);
        }

        #endregion [ DeviceInfo ]
    }
}
