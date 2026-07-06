using MediatR;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.UseCases.Stockpiles.ApplyCityHealthcareMedicineDemand;

public sealed record ApplyCityHealthcareMedicineDemandCommand(
    Guid CityId,
    int ProcessedPatientCount,
    int RoutineCareDeliveryCount,
    int UrgentCareDeliveryCount,
    int AcuteCareDeliveryCount,
    int EmergencyCareDeliveryCount,
    long SourceRevision,
    DateOnly CareDate,
    DateTimeOffset ObservedAtUtc) : IRequest<ApplyCityHealthcareMedicineDemandResult>;
