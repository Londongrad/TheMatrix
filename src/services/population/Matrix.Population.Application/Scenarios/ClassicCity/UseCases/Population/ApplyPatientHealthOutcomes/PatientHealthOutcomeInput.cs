using Matrix.Population.Domain.Enums;

namespace Matrix.Population.Application.Scenarios.ClassicCity.UseCases.Population.ApplyPatientHealthOutcomes
{
    public sealed record PatientHealthOutcomeInput(
        Guid PatientId,
        int HealthScore,
        IllnessKind? CurrentIllnessKind,
        IllnessSeverity? CurrentIllnessSeverity,
        DateOnly? DiagnosedOn,
        DateOnly? LastRecoveredOn,
        int HappinessDelta,
        int EnergyDelta,
        int StressDelta);
}
