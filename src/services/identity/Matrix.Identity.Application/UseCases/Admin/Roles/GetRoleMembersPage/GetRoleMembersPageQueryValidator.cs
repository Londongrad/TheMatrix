using FluentValidation;
using Matrix.BuildingBlocks.Application.Validation;

namespace Matrix.Identity.Application.UseCases.Admin.Roles.GetRoleMembersPage
{
    public sealed class GetRoleMembersPageQueryValidator : AbstractValidator<GetRoleMembersPageQuery>
    {
        public GetRoleMembersPageQueryValidator()
        {
            RuleFor(x => x.RoleId)
               .NotEmpty();

            RuleFor(x => x.Pagination)
               .NotNull()
               .SetValidator(new PaginationValidator());
        }
    }
}
