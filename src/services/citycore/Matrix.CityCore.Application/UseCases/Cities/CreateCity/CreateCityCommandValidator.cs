using FluentValidation;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Application.UseCases.Cities.CreateCity
{
    public sealed class CreateCityCommandValidator : AbstractValidator<CreateCityCommand>
    {
        public CreateCityCommandValidator()
        {
            RuleFor(x => x.Name)
               .NotEmpty()
               .MaximumLength(CityName.MaxLength);

            RuleFor(x => x.StartSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("StartSimTimeUtc must be UTC (Offset=00:00).");

            RuleFor(x => x.SpeedMultiplier)
               .InclusiveBetween(
                    from: SimSpeed.Min,
                    to: SimSpeed.Max);
        }
    }
}
