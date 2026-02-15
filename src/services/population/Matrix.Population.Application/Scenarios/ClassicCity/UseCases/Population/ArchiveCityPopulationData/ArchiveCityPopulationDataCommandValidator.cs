using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ArchiveCityPopulationData
{
    public sealed class ArchiveCityPopulationDataCommandValidator : AbstractValidator<ArchiveCityPopulationDataCommand>
    {
        public ArchiveCityPopulationDataCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.IntegrationMessageId)
               .NotEmpty();

            RuleFor(x => x.ConsumerName)
               .NotEmpty();

            RuleFor(x => x.ArchivedAtUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("ArchivedAtUtc must be in UTC (Offset=00:00).");
        }
    }
}
