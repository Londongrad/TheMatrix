using Matrix.Healthcare.Domain.Patients;
using Matrix.Healthcare.Domain.Progression;
using Matrix.Healthcare.Domain.Simulation;

namespace Matrix.Healthcare.Application.Patients.AdvancePatientHealth
{
    internal static class AdvancePatientHealthBatchPreparer
    {
        internal const int MaxBatchSize = 1000;

        internal static PreparedPatientHealthBatch Prepare(AdvancePatientHealthCommand request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Patients);

            if (request.Patients.Count == 0)
                throw new ArgumentException(
                    message: "A patient health progression batch cannot be empty.",
                    paramName: nameof(request.Patients));
            if (request.Patients.Count > MaxBatchSize)
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(request.Patients),
                    message: $"A patient health progression batch cannot exceed {MaxBatchSize} patients.");
            if (request.SourceRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(request.SourceRevision));
            if (request.CurrentDate < request.PreviousDate)
                throw new ArgumentException(
                    message: "A patient health progression interval cannot move backwards.",
                    paramName: nameof(request.CurrentDate));
            if (request.ObservedAtUtc.Offset != TimeSpan.Zero)
                throw new ArgumentException(
                    message: "Patient health observation timestamps must be expressed in UTC.",
                    paramName: nameof(request.ObservedAtUtc));
            if (string.IsNullOrWhiteSpace(request.CorrelationId))
                throw new ArgumentException(
                    message: "A patient health progression correlation identifier is required.",
                    paramName: nameof(request.CorrelationId));
            if (request.TotalBatches <= 0
                || request.BatchNumber <= 0
                || request.BatchNumber > request.TotalBatches)
                throw new ArgumentException(
                    message: "Patient health progression batch position metadata is invalid.",
                    paramName: nameof(request.BatchNumber));

            var simulationHostId = new SimulationHostId(request.SimulationHostId);
            var patientIds = new HashSet<PatientId>();
            var patients = new PreparedPatientHealthRisk[request.Patients.Count];

            for (int index = 0; index < request.Patients.Count; index++)
            {
                AdvancePatientHealthRiskItem item = request.Patients[index] ??
                                                    throw new ArgumentException(
                                                        "A batch cannot contain null patient risks.",
                                                        nameof(request.Patients));
                var patientId = new PatientId(item.PatientId);
                if (!patientIds.Add(patientId))
                    throw new ArgumentException(
                        message: $"Patient '{patientId}' occurs more than once in a progression batch.",
                        paramName: nameof(request.Patients));

                patients[index] = new PreparedPatientHealthRisk(
                    patientId,
                    new PatientHealthRiskFactors(
                        energyScore: item.EnergyScore,
                        happinessScore: item.HappinessScore,
                        stressScore: item.StressScore,
                        socialNeedScore: item.SocialNeedScore,
                        isVulnerable: item.IsVulnerable,
                        housingStability: item.HousingStability,
                        hasStructuredDailyActivity: item.HasStructuredDailyActivity,
                        infectiousHouseholdContacts: item.InfectiousHouseholdContacts,
                        householdSize: item.HouseholdSize,
                        caregiverSupportStrength: item.CaregiverSupportStrength,
                        hadAdverseWeatherExposure: item.HadAdverseWeatherExposure,
                        healthcareSupportStrength: item.HealthcareSupportStrength,
                        publicHealthRiskStrength: item.PublicHealthRiskStrength,
                        externalHealthDelta: item.ExternalHealthDelta));
            }

            return new PreparedPatientHealthBatch(
                SimulationHostId: simulationHostId,
                SourceRevision: request.SourceRevision,
                PreviousDate: request.PreviousDate,
                CurrentDate: request.CurrentDate,
                ObservedAtUtc: request.ObservedAtUtc,
                CorrelationId: request.CorrelationId,
                BatchNumber: request.BatchNumber,
                TotalBatches: request.TotalBatches,
                PatientIds: patientIds.ToArray(),
                Patients: patients);
        }
    }

    internal sealed record PreparedPatientHealthBatch(
        SimulationHostId SimulationHostId,
        long SourceRevision,
        DateOnly PreviousDate,
        DateOnly CurrentDate,
        DateTimeOffset ObservedAtUtc,
        string CorrelationId,
        int BatchNumber,
        int TotalBatches,
        IReadOnlyCollection<PatientId> PatientIds,
        IReadOnlyList<PreparedPatientHealthRisk> Patients);

    internal sealed record PreparedPatientHealthRisk(
        PatientId PatientId,
        PatientHealthRiskFactors RiskFactors);
}
