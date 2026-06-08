using FluentValidation.Results;
using Matrix.BuildingBlocks.Application.Models;
using Matrix.Population.Application.UseCases.Population.GetPeoplePage;
using Xunit;

namespace Matrix.Population.Application.Tests.UseCases.Population.GetPeoplePage;

public sealed class GetPeoplePageQueryValidatorTests
{
    private readonly GetPeoplePageQueryValidator _validator = new();

    [Fact]
    public void Validate_WithValidPagination_ReturnsNoErrors()
    {
        ValidationResult? result = _validator.Validate(
            new GetPeoplePageQuery(
                new Pagination(
                    pageNumber: 1,
                    pageSize: 50)));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithNullPagination_ReturnsError()
    {
        ValidationResult? result = _validator.Validate(new GetPeoplePageQuery(Pagination: null!));

        Assert.False(result.IsValid);
        Assert.Contains(
            collection: result.Errors,
            filter: x => x.PropertyName == "Pagination");
    }
}
