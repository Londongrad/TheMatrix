namespace Matrix.Healthcare.Domain.Progression
{
    public sealed record PatientHealthRiskFactors
    {
        public PatientHealthRiskFactors(
            int energyScore,
            int happinessScore,
            int stressScore,
            int socialNeedScore,
            bool isVulnerable,
            PatientHousingStability housingStability,
            bool hasStructuredDailyActivity,
            int infectiousHouseholdContacts,
            int householdSize,
            double caregiverSupportStrength,
            bool hadAdverseWeatherExposure,
            double healthcareSupportStrength,
            double publicHealthRiskStrength,
            int externalHealthDelta = 0)
        {
            EnergyScore = EnsureScore(energyScore, nameof(energyScore));
            HappinessScore = EnsureScore(happinessScore, nameof(happinessScore));
            StressScore = EnsureScore(stressScore, nameof(stressScore));
            SocialNeedScore = EnsureScore(socialNeedScore, nameof(socialNeedScore));
            IsVulnerable = isVulnerable;
            HousingStability = Enum.IsDefined(housingStability)
                ? housingStability
                : throw new ArgumentOutOfRangeException(nameof(housingStability));
            HasStructuredDailyActivity = hasStructuredDailyActivity;

            if (householdSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(householdSize));
            if (infectiousHouseholdContacts < 0 || infectiousHouseholdContacts >= householdSize)
                throw new ArgumentOutOfRangeException(nameof(infectiousHouseholdContacts));

            InfectiousHouseholdContacts = infectiousHouseholdContacts;
            HouseholdSize = householdSize;
            CaregiverSupportStrength = EnsureStrength(
                caregiverSupportStrength,
                nameof(caregiverSupportStrength));
            HadAdverseWeatherExposure = hadAdverseWeatherExposure;
            HealthcareSupportStrength = EnsureStrength(
                healthcareSupportStrength,
                nameof(healthcareSupportStrength));
            PublicHealthRiskStrength = EnsureStrength(
                publicHealthRiskStrength,
                nameof(publicHealthRiskStrength));
            ExternalHealthDelta = Math.Clamp(
                externalHealthDelta,
                -Matrix.Healthcare.Domain.Patients.HealthScore.Maximum,
                Matrix.Healthcare.Domain.Patients.HealthScore.Maximum);
        }

        public int EnergyScore { get; }
        public int HappinessScore { get; }
        public int StressScore { get; }
        public int SocialNeedScore { get; }
        public bool IsVulnerable { get; }
        public PatientHousingStability HousingStability { get; }
        public bool HasStructuredDailyActivity { get; }
        public int InfectiousHouseholdContacts { get; }
        public int HouseholdSize { get; }
        public double CaregiverSupportStrength { get; }
        public bool HadAdverseWeatherExposure { get; }
        public double HealthcareSupportStrength { get; }
        public double PublicHealthRiskStrength { get; }
        public int ExternalHealthDelta { get; }

        public PatientHealthRiskFactors WithInfectiousHouseholdContacts(int contactCount)
        {
            return new PatientHealthRiskFactors(
                energyScore: EnergyScore,
                happinessScore: HappinessScore,
                stressScore: StressScore,
                socialNeedScore: SocialNeedScore,
                isVulnerable: IsVulnerable,
                housingStability: HousingStability,
                hasStructuredDailyActivity: HasStructuredDailyActivity,
                infectiousHouseholdContacts: contactCount,
                householdSize: HouseholdSize,
                caregiverSupportStrength: CaregiverSupportStrength,
                hadAdverseWeatherExposure: HadAdverseWeatherExposure,
                healthcareSupportStrength: HealthcareSupportStrength,
                publicHealthRiskStrength: PublicHealthRiskStrength,
                externalHealthDelta: ExternalHealthDelta);
        }

        private static int EnsureScore(int value, string parameterName)
        {
            return value is >= 0 and <= 100
                ? value
                : throw new ArgumentOutOfRangeException(parameterName);
        }

        private static double EnsureStrength(double value, string parameterName)
        {
            return value is >= 0d and <= 1d
                ? value
                : throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
