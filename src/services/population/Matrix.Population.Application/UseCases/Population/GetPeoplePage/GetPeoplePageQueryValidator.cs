using FluentValidation;
using Matrix.BuildingBlocks.Application.Validation;

namespace Matrix.Population.Application.UseCases.Population.GetPeoplePage;

public sealed class GetPeoplePageQueryValidator : AbstractValidator<GetPeoplePageQuery>
{
    public GetPeoplePageQueryValidator()
    {
        RuleFor(x => x.Pagination)
           .NotNull()
           .SetValidator(new PaginationValidator());
    }
}
