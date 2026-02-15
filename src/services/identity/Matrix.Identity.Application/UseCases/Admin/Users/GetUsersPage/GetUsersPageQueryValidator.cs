using FluentValidation;
using Matrix.BuildingBlocks.Application.Validation;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUsersPage
{
    public sealed class GetUsersPageQueryValidator : AbstractValidator<GetUsersPageQuery>
    {
        public GetUsersPageQueryValidator()
        {
            RuleFor(x => x.Pagination)
               .NotNull()
               .SetValidator(new PaginationValidator());
        }
    }
}
