using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Domain.Progression
{
    public sealed record PatientIllnessDiagnosisRisk(
        IllnessKind Kind,
        IllnessSeverity Severity,
        double ChancePerReview);
}
