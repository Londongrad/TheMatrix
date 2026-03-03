using FluentValidation;
using Matrix.BuildingBlocks.Application.Validation;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed class GetMySecurityActivityQueryValidator : AbstractValidator<GetMySecurityActivityQuery>
    {
        public GetMySecurityActivityQueryValidator()
        {
            RuleFor(x => x.Pagination)
               .SetValidator(new PaginationValidator());
        }
    }
}
