using FluentValidation.Results;
using Matrix.Economy.Application.UseCases.Bootstrap.InitializeCityEconomy;
using Xunit;

namespace Matrix.Economy.Application.Tests.UseCases.Bootstrap.InitializeCityEconomy
{
    public sealed class InitializeCityEconomyCommandValidatorTests
    {
        private readonly InitializeCityEconomyCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(CreateCommand());

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidFields_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                CreateCommand() with
                {
                    CityId = Guid.Empty,
                    SimulationKind = new string(
                        c: 'x',
                        count: 65),
                    CreatedAtUtc = new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 6,
                        hour: 11,
                        minute: 0,
                        second: 0,
                        offset: TimeSpan.FromHours(3))
                });

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "SimulationKind");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CreatedAtUtc");
        }

        private static InitializeCityEconomyCommand CreateCommand()
        {
            return new InitializeCityEconomyCommand(
                CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SimulationKind: "ClassicCity",
                EconomyProfile: "baseline",
                CreatedAtUtc: new DateTimeOffset(
                    year: 2048,
                    month: 5,
                    day: 6,
                    hour: 11,
                    minute: 0,
                    second: 0,
                    offset: TimeSpan.Zero));
        }
    }
}
