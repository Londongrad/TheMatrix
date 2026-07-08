using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Domain.Operations;

public sealed class CareSystemPressurePolicy
{
    public CareSystemPressureProfile Assess(
        PatientPopulationHealthBurden burden,
        CareOperationalProfile operations)
    {
        ArgumentNullException.ThrowIfNull(burden);
        ArgumentNullException.ThrowIfNull(operations);

        if (burden.PatientCount == 0)
            return new CareSystemPressureProfile(
                PatientCount: 0,
                ActiveIllnessCount: 0,
                SevereIllnessCount: 0,
                MedicalLoadIndex: 0.20m,
                TriagePressureIndex: 0m,
                RecoverySupportIndex: 1m);

        decimal patientCount = burden.PatientCount;
        decimal weightedIllnessLoad = ((burden.MildIllnessCount * 0.85m) +
                                       (burden.ModerateIllnessCount * 1.55m) +
                                       (burden.SevereIllnessCount * 2.75m)) /
                                      patientCount;
        decimal medicineAvailabilityEffect = (operations.MedicineAvailability.Value - 0.50m) * 0.44m;
        decimal effectiveCapacity = Clamp(
            value: 0.40m +
                   (operations.ServiceQuality.Value * 0.72m) +
                   medicineAvailabilityEffect -
                   (operations.MedicineShortageRisk.Value * 0.38m),
            min: 0.25m,
            max: 2.40m);
        decimal overloadPressure = Math.Max(
            val1: 0m,
            val2: (weightedIllnessLoad * 4.20m) - effectiveCapacity);
        decimal severeCaseShare = burden.SevereIllnessCount / patientCount;

        decimal medicalLoadIndex = RoundIndex(
            Clamp(
                value: 0.20m +
                       (weightedIllnessLoad * 3.60m) +
                       (overloadPressure * 0.65m) +
                       (operations.MedicineShortageRisk.Value * 0.24m),
                min: 0.20m,
                max: 3m));
        decimal triagePressureIndex = RoundIndex(
            Clamp(
                value: (severeCaseShare * 4.40m) +
                       (overloadPressure * 0.90m) +
                       (operations.MedicineShortageRisk.Value * 0.35m),
                min: 0m,
                max: 3m));
        decimal recoverySupportIndex = RoundIndex(
            Clamp(
                value: effectiveCapacity -
                       (overloadPressure * 0.28m) -
                       (operations.MedicineShortageRisk.Value * 0.20m) +
                       (operations.MedicineAvailability.Value * 0.08m),
                min: 0.25m,
                max: 1.75m));

        return new CareSystemPressureProfile(
            PatientCount: burden.PatientCount,
            ActiveIllnessCount: burden.ActiveIllnessCount,
            SevereIllnessCount: burden.SevereIllnessCount,
            MedicalLoadIndex: medicalLoadIndex,
            TriagePressureIndex: triagePressureIndex,
            RecoverySupportIndex: recoverySupportIndex);
    }

    private static decimal Clamp(decimal value, decimal min, decimal max)
    {
        return value < min
            ? min
            : value > max
                ? max
                : value;
    }

    private static decimal RoundIndex(decimal value)
    {
        return decimal.Round(
            d: value,
            decimals: 4,
            mode: MidpointRounding.AwayFromZero);
    }
}
