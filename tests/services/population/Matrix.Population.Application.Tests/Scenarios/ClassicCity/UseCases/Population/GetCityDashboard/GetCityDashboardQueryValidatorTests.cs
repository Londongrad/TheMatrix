using FluentValidation.Results;
using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityDashboard
{
    public sealed class GetCityDashboardQueryValidatorTests
    {
        private readonly GetCityDashboardQueryValidator _validator = new();

        [Fact]
        public void Validate_WithValidQuery_ReturnsNoErrors()
        {
            ValidationResult? result = _validator.Validate(CreateQuery());

            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithEmptyCityId_ReturnsError()
        {
            ValidationResult? result = _validator.Validate(
                CreateQuery() with
                {
                    CityId = Guid.Empty
                });

            Assert.False(result.IsValid);
            Assert.Contains(
                collection: result.Errors,
                filter: x => x.PropertyName == "CityId");
        }

        private static GetCityDashboardQuery CreateQuery()
        {
            return new GetCityDashboardQuery(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        }
    }
}
