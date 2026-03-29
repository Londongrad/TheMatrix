using Matrix.Resources.Application.Scenarios.ClassicCity.Services;

namespace Matrix.Resources.Application.Scenarios.ClassicCity.Abstractions
{
    public interface ICityBudgetAuthorizationClient
    {
        Task<CityBudgetAuthorizationDecision> AuthorizeAsync(
            CityBudgetAuthorizationRequest request,
            CancellationToken cancellationToken);
    }
}
