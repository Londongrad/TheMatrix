using FluentValidation;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.AdvanceCityStockpiles
{
    public sealed class AdvanceCityStockpilesCommandValidator : AbstractValidator<AdvanceCityStockpilesCommand>
    {
        public AdvanceCityStockpilesCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.FromSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("FromSimTimeUtc must be provided in UTC.");

            RuleFor(x => x.ToSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("ToSimTimeUtc must be provided in UTC.");
        }
    }
}
