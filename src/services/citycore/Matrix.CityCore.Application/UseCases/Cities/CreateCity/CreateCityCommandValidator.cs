using FluentValidation;
using Matrix.CityCore.Domain.Cities;
using Matrix.CityCore.Domain.Cities.Enums;
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

            RuleFor(x => x.GenerationSeed)
               .MaximumLength(CityGenerationSeed.MaxLength)
               .When(x => !string.IsNullOrWhiteSpace(x.GenerationSeed));

            RuleFor(x => x.SizeTier)
               .Must(BeValidSizeTier)
               .When(x => !string.IsNullOrWhiteSpace(x.SizeTier))
               .WithMessage("SizeTier is invalid.");

            RuleFor(x => x.UrbanDensity)
               .Must(BeValidUrbanDensity)
               .When(x => !string.IsNullOrWhiteSpace(x.UrbanDensity))
               .WithMessage("UrbanDensity is invalid.");

            RuleFor(x => x.DevelopmentLevel)
               .Must(BeValidDevelopmentLevel)
               .When(x => !string.IsNullOrWhiteSpace(x.DevelopmentLevel))
               .WithMessage("DevelopmentLevel is invalid.");

            RuleFor(x => x.StartSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("StartSimTimeUtc must be UTC (Offset=00:00).");

            RuleFor(x => x.SpeedMultiplier)
               .InclusiveBetween(
                    from: SimSpeed.Min,
                    to: SimSpeed.Max);
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

        private static bool BeValidSizeTier(string? value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out CitySizeTier sizeTier) &&
                   Enum.IsDefined(sizeTier);
        }

        private static bool BeValidUrbanDensity(string? value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out UrbanDensity urbanDensity) &&
                   Enum.IsDefined(urbanDensity);
        }

        private static bool BeValidDevelopmentLevel(string? value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out CityDevelopmentLevel developmentLevel) &&
                   Enum.IsDefined(developmentLevel);
        }

        private static bool BeAlignedToOffsetStep(int value)
        {
            return value % CityUtcOffset.StepMinutes == 0;
        }
    }
}
