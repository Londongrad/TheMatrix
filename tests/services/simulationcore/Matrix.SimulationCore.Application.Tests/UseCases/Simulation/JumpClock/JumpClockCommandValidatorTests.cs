using Matrix.SimulationCore.Application.UseCases.Simulation.JumpClock;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.JumpClock;

public sealed class JumpClockCommandValidatorTests
{
    private readonly JumpClockCommandValidator _validator = new();

    [Fact]
    public void Validate_WithUtcTimestamp_ReturnsNoErrors()
    {
        var result = _validator.Validate(new JumpClockCommand(
            Guid.NewGuid(),
            new DateTimeOffset(2048, 1, 2, 3, 4, 5, TimeSpan.Zero)));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithNonUtcTimestamp_ReturnsExpectedErrors()
    {
        var result = _validator.Validate(new JumpClockCommand(
            Guid.Empty,
            new DateTimeOffset(2048, 1, 2, 3, 4, 5, TimeSpan.FromHours(3))));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "SimulationId");
        Assert.Contains(result.Errors, x => x.PropertyName == "NewSimTimeUtc" && x.ErrorMessage == "NewSimTimeUtc must be in UTC (Offset=00:00).");
    }
}
