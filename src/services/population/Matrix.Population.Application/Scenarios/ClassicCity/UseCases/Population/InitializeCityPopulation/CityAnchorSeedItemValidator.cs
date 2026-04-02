using FluentValidation;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.InitializeCityPopulation
{
    public sealed class CityAnchorSeedItemValidator : AbstractValidator<CityAnchorSeedItem>
    {
        public CityAnchorSeedItemValidator()
        {
            RuleFor(x => x.CityAnchorId)
               .NotEmpty();

            RuleFor(x => x.DistrictId)
               .NotEmpty();

            RuleFor(x => x.AccessRoadNodeId)
               .NotEmpty();

            RuleFor(x => x.Name)
               .NotEmpty()
               .MaximumLength(200);

            RuleFor(x => x.Type)
               .NotEmpty();

            RuleFor(x => x.Capacity)
               .GreaterThanOrEqualTo(0);

            RuleFor(x => x.CreatedAtUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("City anchor CreatedAtUtc must be UTC (Offset=00:00).");
        }
    }
}
