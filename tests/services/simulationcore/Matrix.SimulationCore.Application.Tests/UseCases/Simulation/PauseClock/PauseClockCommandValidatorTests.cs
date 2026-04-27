using Matrix.SimulationCore.Application.UseCases.Simulation.PauseClock;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.PauseClock;

public sealed class PauseClockCommandValidatorTests
{
    private readonly PauseClockCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidSimulationId_ReturnsNoErrors()
    {
        var result = _validator.Validate(new PauseClockCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptySimulationId_ReturnsError()
    {
        var result = _validator.Validate(new PauseClockCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "SimulationId");
    }
}
