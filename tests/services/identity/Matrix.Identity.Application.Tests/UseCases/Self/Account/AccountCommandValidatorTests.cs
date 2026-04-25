using Matrix.Identity.Application.UseCases.Self.Account.ChangeDisplayName;
using Matrix.Identity.Application.UseCases.Self.Account.ChangeUsername;
using Matrix.Identity.Domain.Entities;
using Matrix.Identity.Domain.ValueObjects;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Self.Account;

public sealed class AccountCommandValidatorTests
{
    [Fact]
    public void ChangeDisplayNameValidator_WithNullOrShortDisplayName_ReturnsNoErrors()
    {
        var validator = new ChangeDisplayNameCommandValidator();

        var nullResult = validator.Validate(new ChangeDisplayNameCommand(DisplayName: null));
        var shortResult = validator.Validate(new ChangeDisplayNameCommand(DisplayName: "Neo"));

        Assert.True(nullResult.IsValid);
        Assert.Empty(nullResult.Errors);
        Assert.True(shortResult.IsValid);
        Assert.Empty(shortResult.Errors);
    }

    [Fact]
    public void ChangeDisplayNameValidator_WithTooLongDisplayName_ReturnsExpectedMessage()
    {
        var validator = new ChangeDisplayNameCommandValidator();

        var result = validator.Validate(new ChangeDisplayNameCommand(
            DisplayName: new string('D', User.DisplayNameMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x =>
            x.PropertyName == "DisplayName" &&
            x.ErrorMessage == $"Display name must be at most {User.DisplayNameMaxLength} characters long.");
    }

    [Fact]
    public void ChangeUsernameValidator_WithValidCommand_ReturnsNoErrors()
    {
        var validator = new ChangeUsernameCommandValidator();

        var result = validator.Validate(new ChangeUsernameCommand(
            Username: "neo",
            CurrentPassword: "Pa$$w0rd"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ChangeUsernameValidator_WithMissingPasswordAndShortUsername_ReturnsExpectedMessages()
    {
        var validator = new ChangeUsernameCommandValidator();

        var result = validator.Validate(new ChangeUsernameCommand(
            Username: "ab",
            CurrentPassword: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CurrentPassword" && x.ErrorMessage == "Current password is required.");
        Assert.Contains(result.Errors, x => x.PropertyName == "Username" && x.ErrorMessage == $"Username must be at least {Username.MinLength} characters long.");
    }
}
