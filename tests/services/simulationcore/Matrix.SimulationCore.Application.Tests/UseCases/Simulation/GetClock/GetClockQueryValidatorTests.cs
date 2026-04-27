using Matrix.SimulationCore.Application.UseCases.Simulation.GetClock;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.GetClock;

public sealed class GetClockQueryValidatorTests
{
    private readonly GetClockQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidSimulationId_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetClockQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptySimulationId_ReturnsError()
    {
        var result = _validator.Validate(new GetClockQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "SimulationId");
    }
}
