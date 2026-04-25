using Matrix.Identity.Application.UseCases.Self.Auth.RegisterUser;
using Matrix.Identity.Domain.Rules;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Auth.RegisterUser;

public sealed class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(new RegisterUserCommand(
            Email: "neo@matrix.local",
            Username: "neo",
            Password: "Pa$$w0rd"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidFields_ReturnsExpectedMessages()
    {
        var result = _validator.Validate(new RegisterUserCommand(
            Email: "bad-email",
            Username: "ab",
            Password: "short"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Email" && x.ErrorMessage == "Email format is invalid.");
        Assert.Contains(result.Errors, x => x.PropertyName == "Username" && x.ErrorMessage == $"Username must be at least {Username.MinLength} characters long.");
        Assert.Contains(result.Errors, x => x.PropertyName == "Password" && x.ErrorMessage == $"New password must be at least {PasswordRules.MinLength} characters long.");
        Assert.Contains(result.Errors, x => x.PropertyName == "Password" && x.ErrorMessage == "New password must contain at least one uppercase letter.");
        Assert.Contains(result.Errors, x => x.PropertyName == "Password" && x.ErrorMessage == "New password must contain at least one digit.");
        Assert.Contains(result.Errors, x => x.PropertyName == "Password" && x.ErrorMessage == "New password must contain at least one special character.");
    }
}
