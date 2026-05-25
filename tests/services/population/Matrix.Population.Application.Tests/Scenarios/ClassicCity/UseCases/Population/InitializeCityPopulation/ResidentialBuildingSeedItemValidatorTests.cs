using FluentValidation.Results;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed class ResidentialBuildingSeedItemValidatorTests
    {
        private readonly ResidentialBuildingSeedItemValidator _validator = new();

        [Fact]
        public void Validate_WithValidItem_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(
                new ResidentialBuildingSeedItem(
                    ResidentialBuildingId: Guid.Parse("40000000-0000-0000-0000-000000000001"),
                    DistrictId: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    ResidentCapacity: 12));

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidFields_ReturnsErrors()
        {
            ValidationResult? result = _validator.Validate(
                new ResidentialBuildingSeedItem(
                    ResidentialBuildingId: Guid.Empty,
                    DistrictId: Guid.Empty,
                    ResidentCapacity: 0));

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "ResidentialBuildingId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "DistrictId");
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "ResidentCapacity");
        }
    }
}
