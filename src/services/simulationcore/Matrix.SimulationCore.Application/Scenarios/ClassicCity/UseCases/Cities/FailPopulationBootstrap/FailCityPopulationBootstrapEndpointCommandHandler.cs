using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailPopulationBootstrap
{
    public sealed class FailCityPopulationBootstrapEndpointCommandHandler(IMediator mediator)
        : IRequestHandler<FailCityPopulationBootstrapEndpointCommand, bool>
    {
        public Task<bool> Handle(
            FailCityPopulationBootstrapEndpointCommand request,
            CancellationToken cancellationToken)
        {
            return mediator.Send(
                request: new FailCityPopulationBootstrapCommand(
                    CityId: request.CityId,
                    OperationId: request.OperationId,
                    FailureCode: request.FailureCode),
                cancellationToken: cancellationToken);
        }
    }
}
