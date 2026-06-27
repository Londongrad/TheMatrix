namespace Matrix.Healthcare.Domain.Progression
{
    public readonly record struct PatientIllnessBurden(
        int HealthDelta,
        int HappinessDelta,
        int EnergyDelta,
        int StressDelta)
    {
        public bool HasAnyEffect => HealthDelta != 0
                                    || HappinessDelta != 0
                                    || EnergyDelta != 0
                                    || StressDelta != 0;
    }
}
