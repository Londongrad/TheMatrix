using MediatR;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyPatientHealthOutcomes
{
    public sealed record ApplyPatientHealthOutcomesCommand(
        Guid CityId,
        Guid IntegrationMessageId,
        string ConsumerName,
        long SourceRevision,
        DateOnly CurrentDate,
        DateTimeOffset OccurredAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyCollection<PatientHealthOutcomeInput> Patients)
        : IRequest<ApplyPatientHealthOutcomesResult>;
}
