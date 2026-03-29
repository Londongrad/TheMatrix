using Matrix.Economy.Application.UseCases.GetCityOperationalBudgetPressure;
using MediatR;

namespace Matrix.Economy.Application.UseCases.AuthorizeCityBudgetOperation
{
    public sealed class AuthorizeCityBudgetOperationCommandHandler(ISender sender)
        : IRequestHandler<AuthorizeCityBudgetOperationCommand, CityBudgetOperationAuthorizationDto>
    {
        public async Task<CityBudgetOperationAuthorizationDto> Handle(
            AuthorizeCityBudgetOperationCommand request,
            CancellationToken cancellationToken)
        {
            CityOperationalBudgetPressureDto pressure = await sender.Send(
                request: new GetCityOperationalBudgetPressureQuery(request.CityId),
                cancellationToken: cancellationToken);

            return CityBudgetOperationAuthorizationPolicy.Authorize(
                cityId: request.CityId,
                category: request.Category,
                operationKind: request.OperationKind,
                requestedIntensity: request.RequestedIntensity,
                estimatedAmount: request.EstimatedAmount,
                emergencyOverrideRequested: request.EmergencyOverrideRequested,
                pressure: pressure);
        }
    }
}
