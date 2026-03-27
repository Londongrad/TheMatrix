using FluentValidation;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DispatchCityResupply
{
    public sealed class DispatchCityResupplyCommandValidator : AbstractValidator<DispatchCityResupplyCommand>
    {
        public DispatchCityResupplyCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.Focus)
               .Must(x => Enum.IsDefined(x))
               .WithMessage("Focus must be a valid resupply focus.");

            RuleFor(x => x.Intensity)
               .Must(x => Enum.IsDefined(x))
               .WithMessage("Intensity must be a valid resupply intensity.");
        }
    }
}
