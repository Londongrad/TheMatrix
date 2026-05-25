using FluentValidation.Results;
using Matrix.SimulationCore.Application.UseCases.Simulation.ResumeClock;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.ResumeClock
{
    public sealed class ResumeClockCommandValidatorTests
    {
        private readonly ResumeClockCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidSimulationId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(new ResumeClockCommand(Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptySimulationId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new ResumeClockCommand(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "SimulationId");
        }
    }
}
