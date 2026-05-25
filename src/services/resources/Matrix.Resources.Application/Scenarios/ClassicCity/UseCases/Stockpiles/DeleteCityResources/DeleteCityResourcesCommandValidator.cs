using FluentValidation;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.DeleteCityResources
{
    public sealed class DeleteCityResourcesCommandValidator : AbstractValidator<DeleteCityResourcesCommand>
    {
        public DeleteCityResourcesCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.DeletedAtUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("DeletedAtUtc must be in UTC (Offset=00:00).");
        }
    }
}
