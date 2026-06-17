using Matrix.Economy.Application.Scenarios.ClassicCity.Abstractions;
using Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.GetCityOperationalBudgetPressure;
using MediatR;

namespace Matrix.Economy.Application.Scenarios.ClassicCity.UseCases.AuthorizeCityBudgetOperation
{
    public sealed class AuthorizeCityBudgetOperationCommandHandler(
        ICityOperationalBudgetPressureProjectionService pressureProjectionService)
        : IRequestHandler<AuthorizeCityBudgetOperationCommand, CityBudgetOperationAuthorizationDto>
    {
        public async Task<CityBudgetOperationAuthorizationDto> Handle(
            AuthorizeCityBudgetOperationCommand request,
            CancellationToken cancellationToken)
        {
            CityOperationalBudgetPressureDto pressure = await pressureProjectionService.GetAsync(
                cityId: request.CityId,
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
