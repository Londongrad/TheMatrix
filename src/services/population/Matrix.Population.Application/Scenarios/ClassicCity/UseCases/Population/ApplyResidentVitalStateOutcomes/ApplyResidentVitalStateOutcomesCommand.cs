using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyResidentVitalStateOutcomes
{
    public sealed record ApplyResidentVitalStateOutcomesCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        long SourceRevision,
        DateOnly CurrentDate,
        DateTimeOffset OccurredAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyCollection<ResidentVitalStateOutcomeInput> Residents)
        : IRequest<ApplyResidentVitalStateOutcomesResult>;
}
