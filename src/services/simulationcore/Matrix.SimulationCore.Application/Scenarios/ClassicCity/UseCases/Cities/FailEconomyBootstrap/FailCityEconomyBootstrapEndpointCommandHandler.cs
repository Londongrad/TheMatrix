using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.FailEconomyBootstrap
{
    public sealed class FailCityEconomyBootstrapEndpointCommandHandler(IMediator mediator)
        : IRequestHandler<FailCityEconomyBootstrapEndpointCommand, bool>
    {
        public Task<bool> Handle(
            FailCityEconomyBootstrapEndpointCommand request,
            CancellationToken cancellationToken)
        {
            return mediator.Send(
                request: new FailCityEconomyBootstrapCommand(
                    CityId: request.CityId,
                    OperationId: request.OperationId,
                    FailureCode: request.FailureCode),
                cancellationToken: cancellationToken);
        }
    }
}
