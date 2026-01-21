using FluentValidation;

namespace Matrix.CityCore.Application.UseCases.Cities.ArchiveCity
{
    public sealed class ArchiveCityCommandValidator : AbstractValidator<ArchiveCityCommand>
    {
        public ArchiveCityCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();
        }
    }
}
