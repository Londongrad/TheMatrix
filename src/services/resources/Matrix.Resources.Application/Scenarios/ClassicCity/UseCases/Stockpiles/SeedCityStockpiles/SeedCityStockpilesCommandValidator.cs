using FluentValidation;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.SeedCityStockpiles
{
    public sealed class SeedCityStockpilesCommandValidator : AbstractValidator<SeedCityStockpilesCommand>
    {
        public SeedCityStockpilesCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.CreatedAtUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("CreatedAtUtc must be provided in UTC.");
        }
    }
}
