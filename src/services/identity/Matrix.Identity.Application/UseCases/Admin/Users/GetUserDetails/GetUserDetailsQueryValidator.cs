using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Admin.Users.GetUserDetails
{
    public sealed class GetUserDetailsQueryValidator : AbstractValidator<GetUserDetailsQuery>
    {
        public GetUserDetailsQueryValidator()
        {
            RuleFor(x => x.UserId)
               .NotEmpty();
        }
    }
}
