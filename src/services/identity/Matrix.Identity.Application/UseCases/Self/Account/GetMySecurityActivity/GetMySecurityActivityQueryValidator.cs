using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed class GetMySecurityActivityQueryValidator : AbstractValidator<GetMySecurityActivityQuery>
    {
        public GetMySecurityActivityQueryValidator()
        {
            RuleFor(x => x.Limit)
               .InclusiveBetween(1, 50);
        }
    }
}
