using FluentValidation;

namespace Matrix.Economy.Application.UseCases.Lifecycle.DeleteCityEconomyData
{
    public sealed class DeleteCityEconomyDataCommandValidator : AbstractValidator<DeleteCityEconomyDataCommand>
    {
        public DeleteCityEconomyDataCommandValidator()
        {
            RuleFor(x => x.CityId)
               .NotEmpty();

            RuleFor(x => x.DeletedAtUtc)
               .Must(x => x.Offset == TimeSpan.Zero)
               .WithMessage("DeletedAtUtc must be in UTC (Offset=00:00).");
        }
    }
}
