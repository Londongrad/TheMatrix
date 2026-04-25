using Matrix.Identity.Application.UseCases.Admin.Roles.CreateRole;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.CreateRole;

public sealed class CreateRoleCommandValidatorTests
{
    private readonly CreateRoleCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidName_ReturnsNoErrors()
    {
        var result = _validator.Validate(new CreateRoleCommand("Operators"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsExpectedError()
    {
        var result = _validator.Validate(new CreateRoleCommand(string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Name" && x.ErrorMessage == "Name must not be empty");
    }

    [Fact]
    public void Validate_WithTooLongName_ReturnsExpectedError()
    {
        var result = _validator.Validate(new CreateRoleCommand(new string('R', Role.NameMaxLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Name" && x.ErrorMessage == $"Role name must be at most {Role.NameMaxLength} characters");
    }
}
