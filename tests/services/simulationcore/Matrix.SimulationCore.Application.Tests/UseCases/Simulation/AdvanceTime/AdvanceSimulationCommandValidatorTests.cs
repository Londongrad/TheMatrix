using FluentValidation.Results;
using Matrix.SimulationCore.Application.UseCases.Simulation.AdvanceTime;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.AdvanceTime
{
    public sealed class AdvanceSimulationCommandValidatorTests
    {
        private readonly AdvanceSimulationCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new AdvanceSimulationCommand(
                    SimulationId: Guid.NewGuid(),
                    RealDelta: TimeSpan.FromSeconds(1)));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidCommand_ReturnsExpectedErrors()
        {
            ValidationResult? result = _validator.Validate(
                new AdvanceSimulationCommand(
                    SimulationId: Guid.Empty,
                    RealDelta: TimeSpan.Zero));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "SimulationId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "RealDelta" && x.ErrorMessage == "RealDelta must be greater than zero.");
        }
    }
}
