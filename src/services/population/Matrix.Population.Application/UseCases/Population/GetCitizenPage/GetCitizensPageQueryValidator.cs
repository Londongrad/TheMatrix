using FluentValidation;
using Matrix.BuildingBlocks.Application.Validation;

namespace Matrix.Population.Application.UseCases.Population.GetCitizenPage
{
    public sealed class GetCitizensPageQueryValidator : AbstractValidator<GetCitizensPageQuery>
    {
        public GetCitizensPageQueryValidator()
        {
            RuleFor(x => x.Pagination)
               .NotNull()
               .SetValidator(new PaginationValidator());
        }
    }
}
