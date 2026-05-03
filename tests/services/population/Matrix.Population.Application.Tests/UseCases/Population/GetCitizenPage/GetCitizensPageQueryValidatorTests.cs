using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.UseCases.Population.GetCitizenPage;
using Xunit;

namespace Matrix.Population.Application.Tests.UseCases.Population.GetCitizenPage;

public sealed class GetCitizensPageQueryValidatorTests
{
    private readonly GetCitizensPageQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidPagination_ReturnsNoErrors()
    {
        var result = _validator.Validate(new GetCitizensPageQuery(new Pagination(pageNumber: 1, pageSize: 50)));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithNullPagination_ReturnsError()
    {
        var result = _validator.Validate(new GetCitizensPageQuery(Pagination: null!));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Pagination");
    }
}
