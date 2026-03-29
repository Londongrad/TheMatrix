using Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Services;

namespace Matrix.SimulationSystems.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityBudgetAuthorizationClient
    {
        Task<CityBudgetAuthorizationDecision> AuthorizeAsync(
            CityBudgetAuthorizationRequest request,
            CancellationToken cancellationToken);
    }
}
