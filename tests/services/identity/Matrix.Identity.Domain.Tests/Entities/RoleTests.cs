using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Identity.Domain.Entities;
using Xunit;

namespace Matrix.Identity.Domain.Tests.Entities;

public sealed class RoleTests
{
    private static readonly DateTime CreatedAtUtc = new(2047, 4, 5, 6, 7, 8, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidValues_SetsProperties()
    {
        var role = Role.Create(
            name: " Manager ",
            isSystem: false,
            createdAtUtc: CreatedAtUtc);

        Assert.NotEqual(Guid.Empty, role.Id);
        Assert.Equal("Manager", role.Name);
        Assert.Equal("MANAGER", role.NormalizedName);
        Assert.False(role.IsSystem);
        Assert.Equal(CreatedAtUtc, role.CreatedAtUtc);
    }

    [Fact]
    public void Create_WithWhitespaceName_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => Role.Create(
            name: "   ",
            isSystem: true,
            createdAtUtc: CreatedAtUtc));

        Assert.Equal("Identity.Role.Name.Empty", exception.Code);
        Assert.Equal("name", exception.PropertyName);
    }

    [Fact]
    public void Create_WithTooLongName_ThrowsDomainException()
    {
        var exception = Assert.Throws<DomainException>(() => Role.Create(
            name: new string('R', Role.NameMaxLength + 1),
            isSystem: false,
            createdAtUtc: CreatedAtUtc));

        Assert.Equal("Identity.Role.Name.InvalidLength", exception.Code);
        Assert.Equal("name", exception.PropertyName);
    }

    [Fact]
    public void Rename_WithValidName_UpdatesNameAndNormalizedName()
    {
        var role = Role.Create(
            name: "Manager",
            isSystem: false,
            createdAtUtc: CreatedAtUtc);

        role.Rename(" auditor ");

        Assert.Equal("auditor", role.Name);
        Assert.Equal("AUDITOR", role.NormalizedName);
    }

    [Fact]
    public void MarkAsSystem_SetsFlagToTrue()
    {
        var role = Role.Create(
            name: "Manager",
            isSystem: false,
            createdAtUtc: CreatedAtUtc);

        role.MarkAsSystem();

        Assert.True(role.IsSystem);
    }
}
