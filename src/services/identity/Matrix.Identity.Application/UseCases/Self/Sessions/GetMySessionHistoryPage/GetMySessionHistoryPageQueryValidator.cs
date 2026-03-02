using FluentValidation;
using Matrix.BuildingBlocks.Application.Validation;

namespace Matrix.Identity.Application.UseCases.Self.Sessions.GetMySessionHistoryPage
{
    public sealed class GetMySessionHistoryPageQueryValidator : AbstractValidator<GetMySessionHistoryPageQuery>
    {
        public GetMySessionHistoryPageQueryValidator()
        {
            RuleFor(x => x.Pagination)
               .NotNull()
               .SetValidator(new PaginationValidator());
        }
    }
}
