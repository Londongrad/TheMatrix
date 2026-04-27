using Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceTime;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.AdvanceTime;

public sealed class AdvanceSimulationCommandValidatorTests
{
    private readonly AdvanceSimulationCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoErrors()
    {
        var result = _validator.Validate(new AdvanceSimulationCommand(Guid.NewGuid(), TimeSpan.FromSeconds(1)));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithInvalidCommand_ReturnsExpectedErrors()
    {
        var result = _validator.Validate(new AdvanceSimulationCommand(Guid.Empty, TimeSpan.Zero));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "SimulationId");
        Assert.Contains(result.Errors, x => x.PropertyName == "RealDelta" && x.ErrorMessage == "RealDelta must be greater than zero.");
    }
}
