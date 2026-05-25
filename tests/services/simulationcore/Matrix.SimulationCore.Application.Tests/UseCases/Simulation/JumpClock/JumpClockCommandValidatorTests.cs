using FluentValidation.Results;
using Matrix.SimulationCore.Application.UseCases.Simulation.JumpClock;
using Xunit;

namespace Matrix.SimulationCore.Application.Tests.UseCases.Simulation.JumpClock
{
    public sealed class JumpClockCommandValidatorTests
    {
        private readonly JumpClockCommandValidator _validator = new();

        [Fact]
        public void Validate_WithUtcTimestamp_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new JumpClockCommand(
                    SimulationId: Guid.NewGuid(),
                    NewSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 1,
                        day: 2,
                        hour: 3,
                        minute: 4,
                        second: 5,
                        offset: TimeSpan.Zero)));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithNonUtcTimestamp_ReturnsExpectedErrors()
        {
            ValidationResult? result = _validator.Validate(
                new JumpClockCommand(
                    SimulationId: Guid.Empty,
                    NewSimTimeUtc: new DateTimeOffset(
                        year: 2048,
                        month: 1,
                        day: 2,
                        hour: 3,
                        minute: 4,
                        second: 5,
                        offset: TimeSpan.FromHours(3))));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "SimulationId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "NewSimTimeUtc" &&
                             x.ErrorMessage == "NewSimTimeUtc must be in UTC (Offset=00:00).");
        }
    }
}
