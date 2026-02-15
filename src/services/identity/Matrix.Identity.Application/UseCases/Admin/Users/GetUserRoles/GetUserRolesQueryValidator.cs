using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUserRoles
{
    public sealed class GetUserRolesQueryValidator : AbstractValidator<GetUserRolesQuery>
    {
        public GetUserRolesQueryValidator()
        {
            RuleFor(x => x.UserId)
               .NotEmpty();
        }
    }
}
