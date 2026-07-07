using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Domain.Progression
{
    public sealed class PatientFunctionalCapacityPolicy
    {
        public FunctionalCapacityScore Assess(
            HealthScore health,
            PatientIllnessState illness)
        {
            ArgumentNullException.ThrowIfNull(illness);

            int generalHealthCapacity = health.Value switch
            {
                >= 70 => FunctionalCapacityScore.Maximum,
                >= 50 => 80,
                _ => health.Value
            };

            int illnessCeiling = illness.CurrentSeverity switch
            {
                null => FunctionalCapacityScore.Maximum,
                IllnessSeverity.Mild => 85,
                IllnessSeverity.Moderate => 60,
                IllnessSeverity.Severe => 30,
                _ => throw new ArgumentOutOfRangeException(nameof(illness))
            };

            return new FunctionalCapacityScore(Math.Min(generalHealthCapacity, illnessCeiling));
        }
    }
}
