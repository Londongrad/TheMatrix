using Matrix.Healthcare.Domain.Patients;

namespace Matrix.Healthcare.Domain.Progression
{
    public sealed class PatientIllnessBurdenPolicy
    {
        public PatientIllnessBurden Resolve(
            IllnessKind kind,
            IllnessSeverity severity,
            int reviewWindows,
            double healthcareSupportStrength)
        {
            if (!Enum.IsDefined(kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!Enum.IsDefined(severity))
                throw new ArgumentOutOfRangeException(nameof(severity));
            if (reviewWindows <= 0)
                throw new ArgumentOutOfRangeException(nameof(reviewWindows));
            if (healthcareSupportStrength is < 0d or > 1d)
                throw new ArgumentOutOfRangeException(nameof(healthcareSupportStrength));

            PatientIllnessBurden daily = kind switch
            {
                IllnessKind.Exposure => severity switch
                {
                    IllnessSeverity.Mild => new PatientIllnessBurden(-1, -1, -1, +1),
                    IllnessSeverity.Moderate => new PatientIllnessBurden(-2, -2, -1, +1),
                    IllnessSeverity.Severe => new PatientIllnessBurden(-3, -3, -2, +2),
                    _ => throw new ArgumentOutOfRangeException(nameof(severity))
                },
                IllnessKind.Exhaustion => severity switch
                {
                    IllnessSeverity.Mild => new PatientIllnessBurden(0, -1, -2, +1),
                    IllnessSeverity.Moderate => new PatientIllnessBurden(-1, -2, -3, +2),
                    IllnessSeverity.Severe => new PatientIllnessBurden(-2, -3, -4, +3),
                    _ => throw new ArgumentOutOfRangeException(nameof(severity))
                },
                IllnessKind.Stress => severity switch
                {
                    IllnessSeverity.Mild => new PatientIllnessBurden(0, -1, -1, +2),
                    IllnessSeverity.Moderate => new PatientIllnessBurden(-1, -2, -2, +3),
                    IllnessSeverity.Severe => new PatientIllnessBurden(-2, -3, -2, +4),
                    _ => throw new ArgumentOutOfRangeException(nameof(severity))
                },
                IllnessKind.Infection => severity switch
                {
                    IllnessSeverity.Mild => new PatientIllnessBurden(-1, -1, -1, +1),
                    IllnessSeverity.Moderate => new PatientIllnessBurden(-2, -2, -2, +2),
                    IllnessSeverity.Severe => new PatientIllnessBurden(-3, -3, -3, +3),
                    _ => throw new ArgumentOutOfRangeException(nameof(severity))
                },
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };

            PatientIllnessBurden scaled = Scale(
                daily,
                Math.Clamp(reviewWindows, min: 1, max: 3));
            return Relieve(scaled, healthcareSupportStrength);
        }

        private static PatientIllnessBurden Scale(PatientIllnessBurden burden, int factor)
        {
            return new PatientIllnessBurden(
                HealthDelta: burden.HealthDelta * factor,
                HappinessDelta: burden.HappinessDelta * factor,
                EnergyDelta: burden.EnergyDelta * factor,
                StressDelta: burden.StressDelta * factor);
        }

        private static PatientIllnessBurden Relieve(
            PatientIllnessBurden burden,
            double supportStrength)
        {
            double reliefFactor = Math.Clamp(
                value: 1d - (supportStrength * 0.65d),
                min: 0.60d,
                max: 1d);

            return new PatientIllnessBurden(
                HealthDelta: ScaleSigned(burden.HealthDelta, reliefFactor),
                HappinessDelta: ScaleSigned(burden.HappinessDelta, reliefFactor),
                EnergyDelta: ScaleSigned(burden.EnergyDelta, reliefFactor),
                StressDelta: ScaleSigned(burden.StressDelta, reliefFactor));
        }

        private static int ScaleSigned(int value, double factor)
        {
            if (value == 0)
                return 0;

            int scaled = (int)Math.Round(
                value: value * factor,
                mode: MidpointRounding.AwayFromZero);
            return value < 0
                ? Math.Min(-1, scaled)
                : Math.Max(1, scaled);
        }
    }
}
