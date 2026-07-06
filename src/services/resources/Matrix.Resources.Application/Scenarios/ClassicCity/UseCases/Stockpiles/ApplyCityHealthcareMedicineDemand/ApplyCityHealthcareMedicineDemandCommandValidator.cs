using FluentValidation;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.ApplyCityHealthcareMedicineDemand;

public sealed class ApplyCityHealthcareMedicineDemandCommandValidator
    : AbstractValidator<ApplyCityHealthcareMedicineDemandCommand>
{
    public ApplyCityHealthcareMedicineDemandCommandValidator()
    {
        RuleFor(command => command.CityId).NotEmpty();
        RuleFor(command => command.ProcessedPatientCount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.RoutineCareDeliveryCount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.UrgentCareDeliveryCount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.AcuteCareDeliveryCount).GreaterThanOrEqualTo(0);
        RuleFor(command => command.EmergencyCareDeliveryCount).GreaterThanOrEqualTo(0);
        RuleFor(command => command)
           .Must(command =>
                (long)command.RoutineCareDeliveryCount +
                command.UrgentCareDeliveryCount +
                command.AcuteCareDeliveryCount +
                command.EmergencyCareDeliveryCount <= command.ProcessedPatientCount)
           .WithMessage("Delivered care count cannot exceed the processed patient count.");
        RuleFor(command => command.SourceRevision).GreaterThanOrEqualTo(0);
        RuleFor(command => command.ObservedAtUtc)
           .Must(value => value.Offset == TimeSpan.Zero)
           .WithMessage("ObservedAtUtc must be specified in UTC.");
    }
}
