using Matrix.Economy.Domain.Enums;
using MediatR;

namespace Matrix.Economy.Application.UseCases.AuthorizeCityBudgetOperation
{
    public sealed record AuthorizeCityBudgetOperationCommand(
        Guid CityId,
        CityBudgetCategory Category,
        string OperationKind,
        string RequestedIntensity,
        decimal EstimatedAmount,
        bool EmergencyOverrideRequested) : IRequest<CityBudgetOperationAuthorizationDto>;
}
