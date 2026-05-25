using FluentValidation.Results;
using Matrix.SimulationCore.Application.UseCases.Simulation.SetClockSpeed;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.SetClockSpeed
{
    public sealed class SetClockSpeedCommandValidatorTests
    {
        private readonly SetClockSpeedCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidMultiplier_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new SetClockSpeedCommand(
                    SimulationId: Guid.NewGuid(),
                    Multiplier: 60m));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithOutOfRangeMultiplier_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new SetClockSpeedCommand(
                    SimulationId: Guid.Empty,
                    Multiplier: SimSpeed.Max + 1));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "SimulationId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Multiplier");
        }
    }
}
