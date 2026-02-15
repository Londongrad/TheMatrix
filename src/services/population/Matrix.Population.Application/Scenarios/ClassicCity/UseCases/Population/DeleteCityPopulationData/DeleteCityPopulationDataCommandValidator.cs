using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.DeleteCityPopulationData
{
    public sealed class DeleteCityPopulationDataCommandValidator : AbstractValidator<DeleteCityPopulationDataCommand>
    {
        public DeleteCityPopulationDataCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.IntegrationMessageId)
               .NotEmpty();

            RuleFor(x => x.ConsumerName)
               .NotEmpty();

            RuleFor(x => x.DeletedAtUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("DeletedAtUtc must be in UTC (Offset=00:00).");
        }
    }
}
