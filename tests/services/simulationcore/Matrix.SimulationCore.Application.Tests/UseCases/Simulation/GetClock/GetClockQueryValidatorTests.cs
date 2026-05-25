using FluentValidation.Results;
using Matrix.SimulationCore.Application.UseCases.Simulation.GetClock;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.GetClock
{
    public sealed class GetClockQueryValidatorTests
    {
        private readonly GetClockQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidSimulationId_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(new GetClockQuery(Guid.NewGuid()));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptySimulationId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(new GetClockQuery(Guid.Empty));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "SimulationId");
        }
    }
}
