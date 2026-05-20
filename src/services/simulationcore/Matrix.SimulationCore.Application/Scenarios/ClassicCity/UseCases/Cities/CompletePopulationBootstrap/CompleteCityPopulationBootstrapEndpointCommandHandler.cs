using MediatR;

namespace Matrix.SimulationCore.Application.Scenarios.ClassicCity.UseCases.Cities.CompletePopulationBootstrap
{
    public sealed class CompleteCityPopulationBootstrapEndpointCommandHandler(IMediator mediator)
        : IRequestHandler<CompleteCityPopulationBootstrapEndpointCommand, bool>
    {
        public Task<bool> Handle(
            CompleteCityPopulationBootstrapEndpointCommand request,
            CancellationToken cancellationToken)
        {
            return mediator.Send(
                request: new CompleteCityPopulationBootstrapCommand(
                    CityId: request.CityId,
                    OperationId: request.OperationId),
                cancellationToken: cancellationToken);
        }
    }
}
