using Matrix.Population.Application.UseCases.Person.KillPerson;
using Xunit;

namespace Matrix.Population.Application.Tests.UseCases.Person.KillPerson;

public sealed class KillPersonCommandValidatorTests
{
    private readonly KillPersonCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(new KillPersonCommand(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsError()
    {
        var result = _validator.Validate(new KillPersonCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Id");
    }
}
