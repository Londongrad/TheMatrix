using FluentValidation;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.UseCases.EnvironmentalConditions.
    DeleteCitySystemsData
{
    public sealed class DeleteCitySystemsDataCommandValidator : AbstractValidator<DeleteCitySystemsDataCommand>
    {
        public DeleteCitySystemsDataCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.DeletedAtUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("DeletedAtUtc must be in UTC (Offset=00:00).");
        }
    }
}
