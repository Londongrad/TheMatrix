using FluentValidation.Results;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.SyncCityEnvironment
{
    public sealed class ApplyCityEnvironmentSyncCommandValidatorTests
    {
        private readonly ApplyCityEnvironmentSyncCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new ApplyCityEnvironmentSyncCommand(
                    CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                    ClimateZone: "Continental",
                    Hemisphere: "Southern",
                    UtcOffsetMinutes: -120,
                    SyncedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 17,
                        minute: 5,
                        second: 0,
                        offset: TimeSpan.Zero)));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidFields_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new ApplyCityEnvironmentSyncCommand(
                    CityId: Guid.Empty,
                    ClimateZone: "",
                    Hemisphere: "",
                    UtcOffsetMinutes: -1000,
                    SyncedAtUtc: new DateTimeOffset(
                        year: 2048,
                        month: 5,
                        day: 3,
                        hour: 17,
                        minute: 5,
                        second: 0,
                        offset: TimeSpan.FromHours(-4))));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "ClimateZone");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "Hemisphere");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "UtcOffsetMinutes");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "SyncedAtUtc");
        }
    }
}
