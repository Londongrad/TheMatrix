using Matrix.Identity.Application.UseCases.Admin.Roles.GetRolePermissions;
using Xunit;

namespace Matrix.Identity.Application.Tests.UseCases.Admin.Roles.GetRolePermissions;

public sealed class GetRolePermissionsQueryValidatorTests
{
    private readonly GetRolePermissionsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidRoleId_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetRolePermissionsQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyRoleId_ReturnsError()
    {
        var result = _validator.Validate(new GetRolePermissionsQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "RoleId");
    }
}
