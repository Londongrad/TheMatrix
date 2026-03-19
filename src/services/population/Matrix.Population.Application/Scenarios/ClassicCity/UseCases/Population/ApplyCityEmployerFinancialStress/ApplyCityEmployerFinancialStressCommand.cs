using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyCityEmployerFinancialStress
{
    public sealed record ApplyCityEmployerFinancialStressCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        DateTimeOffset OccurredAtUtc,
        IReadOnlyList<EmployerFinancialStressSnapshotInput> Employers)
        : IRequest<ApplyCityEmployerFinancialStressResult>;
}
