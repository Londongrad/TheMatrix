using Matrix.SimulationCore.Application.UseCases.Simulation.SetClockSpeed;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.SetClockSpeed;

public sealed class SetClockSpeedCommandValidatorTests
{
    private readonly SetClockSpeedCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidMultiplier_ReturnsNoErrors()
    {
        var result = _validator.Validate(new SetClockSpeedCommand(Guid.NewGuid(), 60m));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithOutOfRangeMultiplier_ReturnsErrors()
    {
        var result = _validator.Validate(new SetClockSpeedCommand(Guid.Empty, SimSpeed.Max + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "SimulationId");
        Assert.Contains(result.Errors, x => x.PropertyName == "Multiplier");
    }
}
