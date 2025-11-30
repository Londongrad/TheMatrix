using Matrix.BuildingBlocks.Domain.Exceptions;

namespace Matrix.Identity.Domain.Errors
{
    public static class DomainErrorsFactory
    {
        #region [ User - RefreshToken ]

        public static DomainException InvalidExpireDate(string? propertyName = null) => 
            new(
                code: "Identity.RefreshToken.InvalidExpireDate",
                message: "Refresh token expiration must be in the future.",
                propertyName: propertyName);

        public static DomainException RefreshTokenNotFound(string? propertyName = null) => 
            new(
                code: "Identity.RefreshToken.NotFound",
                message: "Refresh token not found.",
                propertyName: propertyName);

        public static DomainException EmptyPasswordHash(string? propertyName = null) => 
            new(
                code: "Identity.RefreshToken.EmptyPasswordHash",
                message: "Password hash is required.",
                propertyName: propertyName);

        #endregion [ User - RefreshToken ]

        #region [ User - Email ]

        public static DomainException EmptyEmail(string? propertyName = null) => 
            new(
                code: "Identity.Email.EmptyEmail",
                message: "Email is required.",
                propertyName: propertyName);

        public static DomainException InvalidEmailFormat(string? propertyName = null) => 
            new(
                code: "Identity.Email.InvalidEmailFormat",
                message: "Email format is invalid.",
                propertyName: propertyName);

        #endregion [ User - Email ]

        #region [ User - Username ]

        public static DomainException EmptyUsername(string? propertyName = null) => 
            new(
                code: "Identity.Username.EmptyUsername",
                message: "Username is required.",
                propertyName: propertyName);

        public static DomainException InvalidUsernameLength(int actualLength, string? propertyName = null) => 
            new(
                code: "Identity.Username.InvalidUsername",
                message: $"Username must be between 3 and 32 characters. Actual length {actualLength}.",
                propertyName: propertyName);

        #endregion [ User - Username ]
    }
}
