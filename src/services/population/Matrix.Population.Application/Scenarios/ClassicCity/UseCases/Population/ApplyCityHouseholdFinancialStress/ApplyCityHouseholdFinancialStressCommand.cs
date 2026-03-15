using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityHouseholdFinancialStress
{
    public sealed record ApplyCityHouseholdFinancialStressCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        DateTimeOffset OccurredAtUtc,
        IReadOnlyList<HouseholdFinancialStressSnapshotInput> Households)
        : IRequest<ApplyCityHouseholdFinancialStressResult>;
}
