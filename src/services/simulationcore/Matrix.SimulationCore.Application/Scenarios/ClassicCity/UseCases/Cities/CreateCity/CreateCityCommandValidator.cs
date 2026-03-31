using FluentValidation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.Enums;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Weather.ValueObjects;
using Matrix.SimulationCore.Domain.Simulation;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CreateCity
{
    public sealed class CreateCityCommandValidator : AbstractValidator<CreateCityCommand>
    {
        public CreateCityCommandValidator()
        {
            RuleFor(x => x.Name)
               .NotEmpty()
               .MaximumLength(CityName.MaxLength);

            RuleFor(x => x.SimulationKind)
               .Must(BeValidSimulationKind)
               .When(x => !string.IsNullOrWhiteSpace(x.SimulationKind))
               .WithMessage("SimulationKind is invalid.");

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

            RuleFor(x => x.ScenarioModelSetVersion)
               .MaximumLength(ScenarioModelSetVersion.MaxLength)
               .When(x => !string.IsNullOrWhiteSpace(x.ScenarioModelSetVersion));

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

            RuleFor(x => x.EconomyProfile)
               .Must(BeValidEconomyProfile)
               .When(x => !string.IsNullOrWhiteSpace(x.EconomyProfile))
               .WithMessage("EconomyProfile is invalid.");

            RuleFor(x => x.PopulationOccupancyProfile)
               .Must(BeValidPopulationOccupancyProfile)
               .When(x => !string.IsNullOrWhiteSpace(x.PopulationOccupancyProfile))
               .WithMessage("PopulationOccupancyProfile is invalid.");

            RuleFor(x => x.InitialWeatherMode)
               .Must(BeValidInitialWeatherMode)
               .When(x => !string.IsNullOrWhiteSpace(x.InitialWeatherMode))
               .WithMessage("InitialWeatherMode is invalid.");

            RuleFor(x => x.InitialWeatherType)
               .NotEmpty()
               .Must(BeValidWeatherType)
               .When(IsManualInitialWeatherMode)
               .WithMessage("InitialWeatherType is invalid.");

            RuleFor(x => x.InitialWeatherSeverity)
               .NotEmpty()
               .Must(BeValidWeatherSeverity)
               .When(IsManualInitialWeatherMode)
               .WithMessage("InitialWeatherSeverity is invalid.");

            RuleFor(x => x.InitialWeatherTemperatureC)
               .InclusiveBetween(
                    from: TemperatureC.Min,
                    to: TemperatureC.Max)
               .When(x => x.InitialWeatherTemperatureC.HasValue)
               .WithMessage($"InitialWeatherTemperatureC must stay between {TemperatureC.Min} and {TemperatureC.Max}.");

            RuleFor(x => x.StartSimTimeUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("StartSimTimeUtc must be UTC (Offset=00:00).");

            RuleFor(x => x.SpeedMultiplier)
               .InclusiveBetween(
                    from: SimSpeed.Min,
                    to: SimSpeed.Max);

            RuleFor(x => x.PlannedPeopleCount)
               .InclusiveBetween(
                    from: 0,
                    to: CityGenerationProfile.MaxPlannedPeopleCount)
               .When(x => x.PlannedPeopleCount.HasValue)
               .WithMessage(
                    $"PlannedPeopleCount must stay between 0 and {CityGenerationProfile.MaxPlannedPeopleCount}.");
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

        private static bool BeValidSimulationKind(string? value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out SimulationKind simulationKind) &&
                   Enum.IsDefined(simulationKind);
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

        private static bool BeValidEconomyProfile(string? value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out CityEconomyProfile economyProfile) &&
                   Enum.IsDefined(economyProfile);
        }

        private static bool BeValidPopulationOccupancyProfile(string? value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out PopulationOccupancyProfile occupancyProfile) &&
                   Enum.IsDefined(occupancyProfile);
        }

        private static bool BeValidInitialWeatherMode(string? value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out InitialWeatherMode mode) &&
                   Enum.IsDefined(mode);
        }

        private static bool BeValidWeatherType(string? value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out WeatherType weatherType) &&
                   Enum.IsDefined(weatherType);
        }

        private static bool BeValidWeatherSeverity(string? value)
        {
            return Enum.TryParse(
                       value: value,
                       ignoreCase: true,
                       result: out WeatherSeverity weatherSeverity) &&
                   Enum.IsDefined(weatherSeverity);
        }

        private static bool IsManualInitialWeatherMode(CreateCityCommand command)
        {
            return string.Equals(
                a: command.InitialWeatherMode,
                b: nameof(InitialWeatherMode.Manual),
                comparisonType: StringComparison.OrdinalIgnoreCase);
        }

        private static bool BeAlignedToOffsetStep(int value)
        {
            return value % CityUtcOffset.StepMinutes == 0;
        }
    }
}
