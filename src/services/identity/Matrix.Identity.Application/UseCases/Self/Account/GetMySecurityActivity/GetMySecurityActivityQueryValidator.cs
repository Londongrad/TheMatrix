using FluentValidation;

namespace Matrix.Identity.Application.UseCases.Self.Account.GetMySecurityActivity
{
    public sealed class GetMySecurityActivityQueryValidator : AbstractValidator<GetMySecurityActivityQuery>
    {
        public GetMySecurityActivityQueryValidator()
        {
            RuleFor(x => x.PageSize)
               .InclusiveBetween(
                    from: 1,
                    to: SecurityActivityPageSizePolicy.MaxPageSize);
        }
    }
}
