using Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails;
using Xunit;

namespace Matrix.Population.Application.Tests.Scenarios.ClassicCity.UseCases.Population.GetCityResidentDetails;

public sealed class GetCityResidentDetailsQueryValidatorTests
{
    private readonly GetCityResidentDetailsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetCityResidentDetailsQuery(
            CityId: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            PersonId: Guid.Parse("11111111-2222-3333-4444-555555555555"),
            CurrentDate: new DateOnly(2048, 5, 5)));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyIds_ReturnsErrors()
    {
        var result = _validator.Validate(new GetCityResidentDetailsQuery(
            CityId: Guid.Empty,
            PersonId: Guid.Empty,
            CurrentDate: new DateOnly(2048, 5, 5)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "CityId");
        Assert.Contains(result.Errors, x => x.PropertyName == "PersonId");
    }
}
