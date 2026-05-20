using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompleteEconomyBootstrap
{
    public sealed class CompleteCityEconomyBootstrapEndpointCommandHandler(IMediator mediator)
        : IRequestHandler<CompleteCityEconomyBootstrapEndpointCommand, bool>
    {
        public Task<bool> Handle(
            CompleteCityEconomyBootstrapEndpointCommand request,
            CancellationToken cancellationToken)
        {
            return mediator.Send(
                request: new CompleteCityEconomyBootstrapCommand(
                    CityId: request.CityId,
                    OperationId: request.OperationId),
                cancellationToken: cancellationToken);
        }
    }
}
