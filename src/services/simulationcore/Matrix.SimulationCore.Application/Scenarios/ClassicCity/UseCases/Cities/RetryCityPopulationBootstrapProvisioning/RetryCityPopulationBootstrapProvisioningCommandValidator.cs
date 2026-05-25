using FluentValidation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.
    RetryCityPopulationBootstrapProvisioning
{
    public sealed class RetryCityPopulationBootstrapProvisioningCommandValidator
        : AbstractValidator<RetryCityPopulationBootstrapProvisioningCommand>
    {
        public RetryCityPopulationBootstrapProvisioningCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.PlannedPeopleCountOverride)
               .InclusiveBetween(
                    from: 0,
                    to: CityGenerationProfile.MaxPlannedPeopleCount)
               .When(x => x.PlannedPeopleCountOverride.HasValue)
               .WithMessage(
                    $"PlannedPeopleCountOverride must stay between 0 and {CityGenerationProfile.MaxPlannedPeopleCount}.");
        }
    }
}
