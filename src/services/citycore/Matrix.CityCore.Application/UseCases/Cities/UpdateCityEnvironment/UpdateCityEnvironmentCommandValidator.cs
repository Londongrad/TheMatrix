using FluentValidation;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Cities.Enums;

namespace Matrix.CityCore.Application.UseCases.Cities.UpdateCityEnvironment
{
    public sealed class UpdateCityEnvironmentCommandValidator : AbstractValidator<UpdateCityEnvironmentCommand>
    {
        public UpdateCityEnvironmentCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.ClimateZone)
               .NotEmpty()
               .Must(BeValidClimateZone)
               .WithMessage("ClimateZone is invalid.");

            RuleFor(x => x.Hemisphere)
               .NotEmpty()
               .Must(BeValidHemisphere)
               .WithMessage("Hemisphere is invalid.");

            RuleFor(x => x.UtcOffsetMinutes)
               .InclusiveBetween(
                    from: CityUtcOffset.MinMinutes,
                    to: CityUtcOffset.MaxMinutes)
               .Must(BeAlignedToOffsetStep)
               .WithMessage($"UtcOffsetMinutes must align to {CityUtcOffset.StepMinutes}-minute increments.");
        }

        private static bool BeValidClimateZone(string value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out ClimateZone climateZone) &&
                   Enum.IsDefined(climateZone);
        }

        private static bool BeValidHemisphere(string value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out Hemisphere hemisphere) &&
                   Enum.IsDefined(hemisphere);
        }

        private static bool BeAlignedToOffsetStep(int value)
        {
            return value % CityUtcOffset.StepMinutes == 0;
        }
    }
}
