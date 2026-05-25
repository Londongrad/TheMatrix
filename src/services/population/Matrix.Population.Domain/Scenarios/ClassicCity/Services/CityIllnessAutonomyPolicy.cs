using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityIllnessAutonomyPolicy
    {
        public bool Apply(
            Person person,
            IReadOnlyCollection<Person> householdResidents,
            DateOnly previousDate,
            DateOnly currentDate,
            HousingStatus? housingStatus,
            bool hadAdverseWeatherExposure,
            double healthcareSupportStrength,
            double publicHealthRiskStrength)
        {
            ArgumentNullException.ThrowIfNull(person);
            ArgumentNullException.ThrowIfNull(householdResidents);

            if (!person.IsAlive || currentDate <= previousDate)
                return false;

            int reviewWindows = ResolveDailyReviewWindows(
                previousDate: previousDate,
                currentDate: currentDate);
            if (reviewWindows <= 0)
                return false;

            bool changed = false;
            bool diagnosedThisPass = false;

            if (!person.HasActiveIllness)
            {
                IllnessDiagnosisCandidate? candidate = ResolveDiagnosisCandidate(
                    person: person,
                    householdResidents: householdResidents,
                    currentDate: currentDate,
                    reviewWindows: reviewWindows,
                    housingStatus: housingStatus,
                    hadAdverseWeatherExposure: hadAdverseWeatherExposure,
                    publicHealthRiskStrength: publicHealthRiskStrength);

                if (candidate is not null)
                {
                    person.DiagnoseIllness(
                        kind: candidate.Kind,
                        severity: candidate.Severity,
                        currentDate: currentDate);
                    changed = true;
                    diagnosedThisPass = true;
                }
            }

            if (!person.HasActiveIllness ||
                person.CurrentIllnessKind is not
                { } illnessKind)
                return changed;

            if (!diagnosedThisPass &&
                ShouldRecover(
                    person: person,
                    householdResidents: householdResidents,
                    currentDate: currentDate,
                    reviewWindows: reviewWindows,
                    housingStatus: housingStatus,
                    hadAdverseWeatherExposure: hadAdverseWeatherExposure,
                    healthcareSupportStrength: healthcareSupportStrength,
                    publicHealthRiskStrength: publicHealthRiskStrength))
            {
                person.RecoverFromIllness(currentDate);
                person.ChangeHappiness(+2);
                return true;
            }

            if (!diagnosedThisPass &&
                ShouldProgress(
                    person: person,
                    householdResidents: householdResidents,
                    currentDate: currentDate,
                    reviewWindows: reviewWindows,
                    housingStatus: housingStatus,
                    hadAdverseWeatherExposure: hadAdverseWeatherExposure,
                    healthcareSupportStrength: healthcareSupportStrength,
                    publicHealthRiskStrength: publicHealthRiskStrength))
            {
                person.ProgressIllness(NextSeverity(person.CurrentIllnessSeverity));
                changed = true;
            }

            IllnessBurden burden = ResolveBurden(
                kind: illnessKind,
                severity: person.CurrentIllnessSeverity ?? IllnessSeverity.Mild,
                reviewWindows: reviewWindows,
                healthcareSupportStrength: healthcareSupportStrength);

            if (!burden.HasAnyEffect)
                return changed;

            bool hadAliveState = person.IsAlive;
            int previousHealth = person.Health.Value;
            int previousHappiness = person.Happiness.Value;
            int previousEnergy = person.Energy.Value;
            int previousStress = person.Stress.Value;

            if (burden.HealthDelta != 0)
                person.ChangeHealth(
                    delta: burden.HealthDelta,
                    currentDate: currentDate);

            if (person.IsAlive)
            {
                if (burden.HappinessDelta != 0)
                    person.ChangeHappiness(burden.HappinessDelta);
                if (burden.EnergyDelta != 0)
                    person.ChangeEnergy(burden.EnergyDelta);
                if (burden.StressDelta != 0)
                    person.ChangeStress(burden.StressDelta);
            }

            return changed ||
                   previousHealth != person.Health.Value ||
                   previousHappiness != person.Happiness.Value ||
                   previousEnergy != person.Energy.Value ||
                   previousStress != person.Stress.Value ||
                   hadAliveState != person.IsAlive;
        }

        private static IllnessDiagnosisCandidate? ResolveDiagnosisCandidate(
            Person person,
            IReadOnlyCollection<Person> householdResidents,
            DateOnly currentDate,
            int reviewWindows,
            HousingStatus? housingStatus,
            bool hadAdverseWeatherExposure,
            double publicHealthRiskStrength)
        {
            var candidates = new List<IllnessDiagnosisCandidate>(capacity: 4);

            if (hadAdverseWeatherExposure)
            {
                double chance = ResolveExposureIllnessChance(
                    person: person,
                    currentDate: currentDate,
                    housingStatus: housingStatus);
                if (chance > 0d)
                    candidates.Add(
                        new IllnessDiagnosisCandidate(
                            Kind: IllnessKind.Exposure,
                            Severity: ResolveExposureSeverity(
                                person: person,
                                housingStatus: housingStatus),
                            ChancePerReview: chance,
                            Salt: 401));
            }

            double exhaustionChance = ResolveExhaustionIllnessChance(person);
            if (exhaustionChance > 0d)
                candidates.Add(
                    new IllnessDiagnosisCandidate(
                        Kind: IllnessKind.Exhaustion,
                        Severity: ResolveExhaustionSeverity(person),
                        ChancePerReview: exhaustionChance,
                        Salt: 433));

            double stressChance = ResolveStressIllnessChance(person);
            if (stressChance > 0d)
                candidates.Add(
                    new IllnessDiagnosisCandidate(
                        Kind: IllnessKind.Stress,
                        Severity: ResolveStressSeverity(person),
                        ChancePerReview: stressChance,
                        Salt: 467));

            double infectionChance = ResolveInfectionIllnessChance(
                person: person,
                householdResidents: householdResidents,
                currentDate: currentDate,
                housingStatus: housingStatus,
                publicHealthRiskStrength: publicHealthRiskStrength);
            if (infectionChance > 0d)
                candidates.Add(
                    new IllnessDiagnosisCandidate(
                        Kind: IllnessKind.Infection,
                        Severity: ResolveInfectionSeverity(
                            person: person,
                            currentDate: currentDate,
                            housingStatus: housingStatus),
                        ChancePerReview: infectionChance,
                        Salt: 479));

            foreach (IllnessDiagnosisCandidate candidate in candidates.OrderByDescending(x => x.ChancePerReview))
                if (RollOccurs(
                        personId: person.Id,
                        currentDate: currentDate,
                        salt: candidate.Salt,
                        chancePerReview: candidate.ChancePerReview,
                        reviewWindows: reviewWindows))
                    return candidate;

            return null;
        }

        private static bool ShouldRecover(
            Person person,
            IReadOnlyCollection<Person> householdResidents,
            DateOnly currentDate,
            int reviewWindows,
            HousingStatus? housingStatus,
            bool hadAdverseWeatherExposure,
            double healthcareSupportStrength,
            double publicHealthRiskStrength)
        {
            if (!person.HasActiveIllness ||
                person.CurrentIllnessSeverity is not
                { } severity)
                return false;

            double health = Normalize(person.Health.Value);
            double happiness = Normalize(person.Happiness.Value);
            double energy = Normalize(person.Energy.Value);
            double stress = Normalize(person.Stress.Value);
            bool housed = housingStatus == HousingStatus.Housed;
            double careSupport = ResolveCareSupportStrength(
                resident: person,
                householdResidents: householdResidents,
                currentDate: currentDate);

            double baseChance = severity switch
            {
                IllnessSeverity.Mild => 0.16d,
                IllnessSeverity.Moderate => 0.08d,
                IllnessSeverity.Severe => 0.03d,
                _ => 0.04d
            };

            double chance = baseChance +
                            (health * 0.08d) +
                            (energy * 0.04d) +
                            (happiness * 0.02d) -
                            (stress * 0.06d) +
                            (housed
                                ? 0.03d
                                : -0.02d) +
                            (careSupport * 0.08d) +
                            (healthcareSupportStrength * 0.12d) -
                            (publicHealthRiskStrength * 0.06d) -
                            (hadAdverseWeatherExposure
                                ? 0.04d
                                : 0d);

            if (person.Health.Value < 35 || person.Energy.Value < 25)
                chance *= 0.45d;

            return RollOccurs(
                personId: person.Id,
                currentDate: currentDate,
                salt: 503,
                chancePerReview: Math.Clamp(
                    value: chance,
                    min: 0.005d,
                    max: 0.35d),
                reviewWindows: reviewWindows);
        }

        private static bool ShouldProgress(
            Person person,
            IReadOnlyCollection<Person> householdResidents,
            DateOnly currentDate,
            int reviewWindows,
            HousingStatus? housingStatus,
            bool hadAdverseWeatherExposure,
            double healthcareSupportStrength,
            double publicHealthRiskStrength)
        {
            if (!person.HasActiveIllness || person.CurrentIllnessSeverity == IllnessSeverity.Severe)
                return false;

            double lowHealth = 1d - Normalize(person.Health.Value);
            double lowEnergy = 1d - Normalize(person.Energy.Value);
            double stress = Normalize(person.Stress.Value);
            double careSupport = ResolveCareSupportStrength(
                resident: person,
                householdResidents: householdResidents,
                currentDate: currentDate);

            double chance = 0.004d +
                            (lowHealth * 0.030d) +
                            (lowEnergy * 0.020d) +
                            (stress * 0.016d) +
                            (publicHealthRiskStrength * 0.050d) +
                            (housingStatus == HousingStatus.Homeless
                                ? 0.010d
                                : 0d) +
                            (hadAdverseWeatherExposure && person.CurrentIllnessKind == IllnessKind.Exposure
                                ? 0.020d
                                : 0d) -
                            (careSupport * 0.045d) -
                            (healthcareSupportStrength * 0.060d);

            return RollOccurs(
                personId: person.Id,
                currentDate: currentDate,
                salt: 541,
                chancePerReview: Math.Clamp(
                    value: chance,
                    min: 0.002d,
                    max: 0.18d),
                reviewWindows: reviewWindows);
        }

        private static double ResolveCareSupportStrength(
            Person resident,
            IReadOnlyCollection<Person> householdResidents,
            DateOnly currentDate)
        {
            IEnumerable<Person> caregivers = householdResidents.Where(x =>
                x.Id != resident.Id &&
                x.IsAlive &&
                x.GetAgeGroup(currentDate) is AgeGroup.Adult or AgeGroup.Senior &&
                (!x.HasActiveIllness || x.CurrentIllnessSeverity != IllnessSeverity.Severe));

            int caregiverCount = caregivers.Count();
            if (caregiverCount == 0)
                return 0d;

            double strength = Math.Min(
                val1: 0.18d,
                val2: caregiverCount * 0.06d);

            bool vulnerable = resident.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior;
            if (vulnerable)
                strength += 0.04d;

            bool hasFamilyCaregiver = caregivers.Any(x =>
                x.Id == resident.SpouseId ||
                x.Id == resident.MotherId ||
                x.Id == resident.FatherId ||
                resident.MotherId == x.Id ||
                resident.FatherId == x.Id);

            if (hasFamilyCaregiver)
                strength += 0.03d;

            return Math.Clamp(
                value: strength,
                min: 0d,
                max: 0.28d);
        }

        private static IllnessBurden ResolveBurden(
            IllnessKind kind,
            IllnessSeverity severity,
            int reviewWindows,
            double healthcareSupportStrength)
        {
            IllnessBurden daily = kind switch
            {
                IllnessKind.Exposure => severity switch
                {
                    IllnessSeverity.Mild => new IllnessBurden(
                        HealthDelta: -1,
                        HappinessDelta: -1,
                        EnergyDelta: -1,
                        StressDelta: +1),
                    IllnessSeverity.Moderate => new IllnessBurden(
                        HealthDelta: -2,
                        HappinessDelta: -2,
                        EnergyDelta: -1,
                        StressDelta: +1),
                    IllnessSeverity.Severe => new IllnessBurden(
                        HealthDelta: -3,
                        HappinessDelta: -3,
                        EnergyDelta: -2,
                        StressDelta: +2),
                    _ => IllnessBurden.None
                },
                IllnessKind.Exhaustion => severity switch
                {
                    IllnessSeverity.Mild => new IllnessBurden(
                        HealthDelta: 0,
                        HappinessDelta: -1,
                        EnergyDelta: -2,
                        StressDelta: +1),
                    IllnessSeverity.Moderate => new IllnessBurden(
                        HealthDelta: -1,
                        HappinessDelta: -2,
                        EnergyDelta: -3,
                        StressDelta: +2),
                    IllnessSeverity.Severe => new IllnessBurden(
                        HealthDelta: -2,
                        HappinessDelta: -3,
                        EnergyDelta: -4,
                        StressDelta: +3),
                    _ => IllnessBurden.None
                },
                IllnessKind.Stress => severity switch
                {
                    IllnessSeverity.Mild => new IllnessBurden(
                        HealthDelta: 0,
                        HappinessDelta: -1,
                        EnergyDelta: -1,
                        StressDelta: +2),
                    IllnessSeverity.Moderate => new IllnessBurden(
                        HealthDelta: -1,
                        HappinessDelta: -2,
                        EnergyDelta: -2,
                        StressDelta: +3),
                    IllnessSeverity.Severe => new IllnessBurden(
                        HealthDelta: -2,
                        HappinessDelta: -3,
                        EnergyDelta: -2,
                        StressDelta: +4),
                    _ => IllnessBurden.None
                },
                IllnessKind.Infection => severity switch
                {
                    IllnessSeverity.Mild => new IllnessBurden(
                        HealthDelta: -1,
                        HappinessDelta: -1,
                        EnergyDelta: -1,
                        StressDelta: +1),
                    IllnessSeverity.Moderate => new IllnessBurden(
                        HealthDelta: -2,
                        HappinessDelta: -2,
                        EnergyDelta: -2,
                        StressDelta: +2),
                    IllnessSeverity.Severe => new IllnessBurden(
                        HealthDelta: -3,
                        HappinessDelta: -3,
                        EnergyDelta: -3,
                        StressDelta: +3),
                    _ => IllnessBurden.None
                },
                _ => IllnessBurden.None
            };

            IllnessBurden scaled = daily.Scale(
                Math.Clamp(
                    value: reviewWindows,
                    min: 1,
                    max: 3));
            return scaled.Relieve(healthcareSupportStrength);
        }

        private static double ResolveExposureIllnessChance(
            Person person,
            DateOnly currentDate,
            HousingStatus? housingStatus)
        {
            double lowHealth = 1d - Normalize(person.Health.Value);
            double lowEnergy = 1d - Normalize(person.Energy.Value);
            bool sensitive = person.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior;

            double chance = 0.002d +
                            (housingStatus == HousingStatus.Homeless
                                ? 0.024d
                                : 0d) +
                            (sensitive
                                ? 0.012d
                                : 0d) +
                            (lowHealth * 0.018d) +
                            (lowEnergy * 0.010d);

            return Math.Clamp(
                value: chance,
                min: 0d,
                max: 0.120d);
        }

        private static double ResolveExhaustionIllnessChance(Person person)
        {
            double lowEnergy = 1d - Normalize(person.Energy.Value);
            double stress = Normalize(person.Stress.Value);
            double lowHealth = 1d - Normalize(person.Health.Value);

            double activityModifier = person.Employment.Status switch
            {
                EmploymentStatus.Employed => 0.010d,
                EmploymentStatus.Student => 0.008d,
                _ => 0d
            };

            double chance = 0.001d + (lowEnergy * 0.030d) + (stress * 0.016d) + (lowHealth * 0.008d) + activityModifier;

            return Math.Clamp(
                value: chance,
                min: 0d,
                max: 0.100d);
        }

        private static double ResolveStressIllnessChance(Person person)
        {
            double stress = Normalize(person.Stress.Value);
            double lowHappiness = 1d - Normalize(person.Happiness.Value);
            double socialNeed = Normalize(person.SocialNeed.Value);

            double chance = 0.001d + (stress * 0.028d) + (lowHappiness * 0.018d) + (socialNeed * 0.012d);

            return Math.Clamp(
                value: chance,
                min: 0d,
                max: 0.110d);
        }

        private static double ResolveInfectionIllnessChance(
            Person person,
            IReadOnlyCollection<Person> householdResidents,
            DateOnly currentDate,
            HousingStatus? housingStatus,
            double publicHealthRiskStrength)
        {
            int infectiousContacts = householdResidents.Count(x =>
                x.Id != person.Id &&
                x.IsAlive &&
                x.CurrentIllnessKind == IllnessKind.Infection);

            bool vulnerable = person.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior;
            double lowHealth = 1d - Normalize(person.Health.Value);
            double lowEnergy = 1d - Normalize(person.Energy.Value);
            double stress = Normalize(person.Stress.Value);

            double chance = 0.0006d +
                            (infectiousContacts * 0.020d) +
                            (vulnerable
                                ? 0.008d
                                : 0d) +
                            (housingStatus == HousingStatus.Homeless
                                ? 0.006d
                                : 0d) +
                            (lowHealth * 0.012d) +
                            (lowEnergy * 0.008d) +
                            (stress * 0.006d) +
                            (publicHealthRiskStrength * 0.120d);

            if (householdResidents.Count >= 5)
                chance += 0.006d;

            return Math.Clamp(
                value: chance,
                min: 0d,
                max: 0.160d);
        }

        private static IllnessSeverity ResolveExposureSeverity(
            Person person,
            HousingStatus? housingStatus)
        {
            if (housingStatus == HousingStatus.Homeless && person.Health.Value < 45)
                return IllnessSeverity.Severe;

            if (housingStatus == HousingStatus.Homeless || person.Health.Value < 60 || person.Energy.Value < 45)
                return IllnessSeverity.Moderate;

            return IllnessSeverity.Mild;
        }

        private static IllnessSeverity ResolveExhaustionSeverity(Person person)
        {
            if (person.Energy.Value < 10 || person.Health.Value < 25)
                return IllnessSeverity.Severe;

            if (person.Energy.Value < 25 || person.Stress.Value > 75)
                return IllnessSeverity.Moderate;

            return IllnessSeverity.Mild;
        }

        private static IllnessSeverity ResolveStressSeverity(Person person)
        {
            if (person.Stress.Value > 92 || person.Happiness.Value < 15)
                return IllnessSeverity.Severe;

            if (person.Stress.Value > 78 || person.Happiness.Value < 35)
                return IllnessSeverity.Moderate;

            return IllnessSeverity.Mild;
        }

        private static IllnessSeverity ResolveInfectionSeverity(
            Person person,
            DateOnly currentDate,
            HousingStatus? housingStatus)
        {
            bool vulnerable = person.GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior;

            if ((vulnerable && person.Health.Value < 45) ||
                (housingStatus == HousingStatus.Homeless && person.Health.Value < 55))
                return IllnessSeverity.Severe;

            if (vulnerable ||
                person.Health.Value < 65 ||
                person.Energy.Value < 45 ||
                housingStatus == HousingStatus.Homeless)
                return IllnessSeverity.Moderate;

            return IllnessSeverity.Mild;
        }

        private static int ResolveDailyReviewWindows(
            DateOnly previousDate,
            DateOnly currentDate)
        {
            return Math.Clamp(
                value: currentDate.DayNumber - previousDate.DayNumber,
                min: 0,
                max: 7);
        }

        private static IllnessSeverity NextSeverity(IllnessSeverity? severity)
        {
            return severity switch
            {
                IllnessSeverity.Mild => IllnessSeverity.Moderate,
                IllnessSeverity.Moderate => IllnessSeverity.Severe,
                _ => IllnessSeverity.Severe
            };
        }

        private static bool RollOccurs(
            PersonId personId,
            DateOnly currentDate,
            int salt,
            double chancePerReview,
            int reviewWindows)
        {
            if (reviewWindows <= 0 || chancePerReview <= 0d)
                return false;

            double combinedChance = 1d -
                                    Math.Pow(
                                        x: 1d - chancePerReview,
                                        y: reviewWindows);
            return GetStableFraction(
                       personId: personId,
                       currentDate: currentDate,
                       salt: salt) <
                   combinedChance;
        }

        private static double Normalize(int value)
        {
            return Math.Clamp(
                value: value / 100d,
                min: 0d,
                max: 1d);
        }

        private static double GetStableFraction(
            PersonId personId,
            DateOnly currentDate,
            int salt)
        {
            return GetStableInt(
                       personId: personId,
                       currentDate: currentDate,
                       salt: salt,
                       modulus: 10_000) /
                   10_000d;
        }

        private static int GetStableInt(
            PersonId personId,
            DateOnly currentDate,
            int salt,
            int modulus)
        {
            if (modulus <= 0)
                return 0;

            unchecked
            {
                byte[] bytes = personId.Value.ToByteArray();
                int hash = 19;
                for (int i = 0; i < bytes.Length; i++)
                    hash = (hash * 31) + bytes[i];

                hash = (hash * 31) + currentDate.DayNumber;
                hash = (hash * 31) + salt;

                return (int)(Math.Abs((long)hash) % modulus);
            }
        }

        private sealed record IllnessDiagnosisCandidate(
            IllnessKind Kind,
            IllnessSeverity Severity,
            double ChancePerReview,
            int Salt);

        private readonly record struct IllnessBurden(
            int HealthDelta,
            int HappinessDelta,
            int EnergyDelta,
            int StressDelta)
        {
            public static IllnessBurden None => new(
                HealthDelta: 0,
                HappinessDelta: 0,
                EnergyDelta: 0,
                StressDelta: 0);

            public bool HasAnyEffect =>
                HealthDelta != 0 ||
                HappinessDelta != 0 ||
                EnergyDelta != 0 ||
                StressDelta != 0;

            public IllnessBurden Scale(int factor)
            {
                return new IllnessBurden(
                    HealthDelta: HealthDelta * factor,
                    HappinessDelta: HappinessDelta * factor,
                    EnergyDelta: EnergyDelta * factor,
                    StressDelta: StressDelta * factor);
            }

            public IllnessBurden Relieve(double supportStrength)
            {
                double reliefFactor = Math.Clamp(
                    value: 1d - (supportStrength * 0.65d),
                    min: 0.60d,
                    max: 1d);

                return new IllnessBurden(
                    HealthDelta: ScaleSigned(
                        value: HealthDelta,
                        factor: reliefFactor),
                    HappinessDelta: ScaleSigned(
                        value: HappinessDelta,
                        factor: reliefFactor),
                    EnergyDelta: ScaleSigned(
                        value: EnergyDelta,
                        factor: reliefFactor),
                    StressDelta: ScaleSigned(
                        value: StressDelta,
                        factor: reliefFactor));
            }

            private static int ScaleSigned(
                int value,
                double factor)
            {
                if (value == 0)
                    return 0;

                int scaled = (int)Math.Round(
                    value: value * factor,
                    mode: MidpointRounding.AwayFromZero);
                return value < 0
                    ? Math.Min(
                        val1: -1,
                        val2: scaled)
                    : Math.Max(
                        val1: 1,
                        val2: scaled);
            }
        }
    }
}
