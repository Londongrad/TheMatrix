using FluentValidation;
using Matrix.BuildingBlocks.Application.Models;

namespace Matrix.BuildingBlocks.Application.Validation
{
    public sealed class PaginationValidator : AbstractValidator<Pagination>
    {
        public PaginationValidator()
        {
            RuleFor(x => x.PageNumber)
               .GreaterThan(0);

            RuleFor(x => x.PageSize)
               .InclusiveBetween(
                    from: 1,
                    to: Pagination.MaxPageSize);
        }
    }
}
