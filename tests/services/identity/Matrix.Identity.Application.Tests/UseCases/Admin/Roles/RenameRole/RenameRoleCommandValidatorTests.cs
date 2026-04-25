using Matrix.Identity.Application.UseCases.Admin.Roles.RenameRole;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.RenameRole;

public sealed class RenameRoleCommandValidatorTests
{
    private readonly RenameRoleCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(new RenameRoleCommand(Guid.NewGuid(), "Operators"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyRoleId_ReturnsError()
    {
        var result = _validator.Validate(new RenameRoleCommand(Guid.Empty, "Operators"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "RoleId" && x.ErrorMessage == "RoleId must not be empty");
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsNameError()
    {
        var result = _validator.Validate(new RenameRoleCommand(Guid.NewGuid(), string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Name");
    }
}
