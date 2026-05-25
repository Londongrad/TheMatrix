using FluentValidation.Results;
using Matrix.Identity.Application.UseCases.Self.Auth.LoginUser;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.LoginUser
{
    public sealed class LoginUserCommandValidatorTests
    {
        private readonly LoginUserCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new LoginUserCommand(
                    Login: "neo",
                    Password: "Pa$$w0rd",
                    DeviceId: "device-1",
                    DeviceName: "Phone",
                    UserAgent: "Mozilla/5.0",
                    IpAddress: "127.0.0.1",
                    RememberMe: true));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithMissingRequiredFields_ReturnsErrorsForEachField()
        {
            ValidationResult? result = _validator.Validate(
                new LoginUserCommand(
                    Login: "",
                    Password: "",
                    DeviceId: "",
                    DeviceName: "",
                    UserAgent: "",
                    IpAddress: null,
                    RememberMe: false));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Login");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Password");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "DeviceId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "DeviceName");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "UserAgent");
        }
    }
}
