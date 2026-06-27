namespace Matrix.Healthcare.Domain.Progression
{
    public sealed record PatientIllnessProgressionOutcome(
        bool MedicalStateChanged,
        int HealthDelta,
        int HappinessDelta,
        int EnergyDelta,
        int StressDelta,
        bool BecameCritical)
    {
        public bool HasAnyEffect => MedicalStateChanged
                                    || HealthDelta != 0
                                    || HappinessDelta != 0
                                    || EnergyDelta != 0
                                    || StressDelta != 0
                                    || BecameCritical;
    }
}
