using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Services
{
    public sealed class CityPopulationBootstrapGenerator
    {
        private static readonly string[] JobTitles =
        {
            "Software Engineer",
            "Teacher",
            "Doctor",
            "Nurse",
            "Builder",
            "Store Clerk",
            "Taxi Driver",
            "Accountant",
            "Electrician",
            "Plumber"
        };

        private static readonly string[] MaleFirstNames =
        {
            "Ivan",
            "Alexey",
            "Mikhail",
            "Dmitry",
            "Sergey",
            "Pavel",
            "Nikolay",
            "Andrey"
        };

        private static readonly string[] FemaleFirstNames =
        {
            "Anna",
            "Maria",
            "Ekaterina",
            "Olga",
            "Natalya",
            "Elena",
            "Irina",
            "Sofia"
        };

        private static readonly string[] LastNames =
        {
            "Ivanov",
            "Petrov",
            "Sidorov",
            "Smirnov",
            "Kovalev",
            "Volkov",
            "Morozov",
            "Lebedev"
        };

        public PopulationBootstrapResult GenerateStandalone(
            int peopleCount,
            DateOnly currentDate,
            DateTimeOffset createdAtUtc,
            int? randomSeed = null)
        {
            if (peopleCount <= 0)
                return new PopulationBootstrapResult(
                    Households: Array.Empty<Household>(),
                    Persons: Array.Empty<Person>());

            Random random = CreateRandom(randomSeed);
            var households = new List<Household>(peopleCount);
            var persons = new List<Person>(peopleCount);

            for (int i = 0; i < peopleCount; i++)
            {
                var householdId = HouseholdId.New();
                var household = Household.CreateHomeless(
                    id: householdId,
                    size: HouseholdSize.From(1),
                    createdAtUtc: createdAtUtc);

                households.Add(household);
                persons.Add(
                    CreateRandomPerson(
                        random: random,
                        householdId: householdId,
                        currentDate: currentDate));
            }

            return new PopulationBootstrapResult(
                Households: households,
                Persons: persons);
        }

        public PopulationBootstrapResult GenerateForCity(
            CityId cityId,
            IReadOnlyCollection<ResidentialBuildingResidence> residentialBuildings,
            int peopleCount,
            DateOnly currentDate,
            DateTimeOffset createdAtUtc,
            int? randomSeed = null)
        {
            if (peopleCount <= 0)
                return new PopulationBootstrapResult(
                    Households: Array.Empty<Household>(),
                    Persons: Array.Empty<Person>());

            Random random = CreateRandom(randomSeed);
            var households = new List<Household>();
            var persons = new List<Person>(peopleCount);
            var capacityStates = residentialBuildings
               .Select(x => new BuildingCapacityState(
                    buildingId: x.ResidentialBuildingId,
                    districtId: x.DistrictId,
                    remainingCapacity: x.ResidentCapacity))
               .ToList();

            int remainingPeople = peopleCount;
            while (remainingPeople > 0)
            {
                int householdSizeValue = NextHouseholdSize(
                    random: random,
                    remainingPeople: remainingPeople);
                var householdSize = HouseholdSize.From(householdSizeValue);
                var householdId = HouseholdId.New();
                Household household = TryAllocateHousehold(
                    cityId: cityId,
                    householdId: householdId,
                    householdSize: householdSize,
                    createdAtUtc: createdAtUtc,
                    capacityStates: capacityStates,
                    random: random);

                households.Add(household);

                for (int memberIndex = 0; memberIndex < householdSizeValue; memberIndex++)
                    persons.Add(
                        CreateRandomPerson(
                            random: random,
                            householdId: household.Id,
                            currentDate: currentDate));

                remainingPeople -= householdSizeValue;
            }

            return new PopulationBootstrapResult(
                Households: households,
                Persons: persons);
        }

        private static Household TryAllocateHousehold(
            CityId cityId,
            HouseholdId householdId,
            HouseholdSize householdSize,
            DateTimeOffset createdAtUtc,
            List<BuildingCapacityState> capacityStates,
            Random random)
        {
            var candidates = capacityStates
               .Where(x => x.RemainingCapacity >= householdSize.Value)
               .ToList();

            if (candidates.Count == 0)
                return Household.CreateHomeless(
                    id: householdId,
                    size: householdSize,
                    createdAtUtc: createdAtUtc,
                    cityId: cityId);

            BuildingCapacityState selected = candidates[random.Next(candidates.Count)];
            selected.RemainingCapacity -= householdSize.Value;

            return Household.CreateHoused(
                id: householdId,
                cityId: cityId,
                districtId: selected.DistrictId,
                residentialBuildingId: selected.BuildingId,
                size: householdSize,
                createdAtUtc: createdAtUtc);
        }

        private static int NextHouseholdSize(
            Random random,
            int remainingPeople)
        {
            int[] weightedSizes =
            {
                1,
                1,
                2,
                2,
                2,
                3,
                3,
                4,
                4,
                5
            };
            int selected = weightedSizes[random.Next(weightedSizes.Length)];
            return Math.Min(
                val1: selected,
                val2: remainingPeople);
        }

        private static Random CreateRandom(int? randomSeed)
        {
            return randomSeed.HasValue
                ? new Random(randomSeed.Value)
                : new Random();
        }

        private static Person CreateRandomPerson(
            Random random,
            HouseholdId householdId,
            DateOnly currentDate)
        {
            var personId = PersonId.New();
            Sex sex = CreateRandomSex(random);
            PersonName name = CreateRandomName(
                random: random,
                sex: sex);
            int ageYears = CreateRandomAgeYears(random);
            DateOnly birthDate = currentDate.AddYears(-ageYears);
            var age = Age.FromYears(ageYears);
            AgeGroup ageGroup = AgeGroupRules.GetAgeGroup(age);
            HealthLevel health = CreateRandomHealth(
                random: random,
                ageYears: ageYears);
            BodyWeight weight = CreateRandomWeight(
                random: random,
                sex: sex,
                ageYears: ageYears);
            var personality = Personality.CreateRandom(random);
            EmploymentStatus employmentStatus = CreateRandomEmploymentStatus(
                random: random,
                ageGroup: ageGroup);
            Job? job = employmentStatus == EmploymentStatus.Employed
                ? CreateRandomJob(random)
                : null;
            HappinessLevel happiness = CreateInitialHappiness(
                random: random,
                ageGroup: ageGroup,
                employmentStatus: employmentStatus);
            EducationLevel educationLevel = CreateRandomEducationLevel(
                random: random,
                ageYears: ageYears);
            MaritalStatus maritalStatus = CreateRandomMaritalStatus(
                random: random,
                ageGroup: ageGroup,
                ageYears: ageYears);

            return Person.CreatePerson(
                id: personId,
                householdId: householdId,
                name: name,
                sex: sex,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: maritalStatus,
                spouseId: null,
                educationLevel: educationLevel,
                employmentStatus: employmentStatus,
                happinessLevel: happiness,
                personality: personality,
                birthDate: birthDate,
                healthLevel: health,
                weight: weight,
                job: job,
                currentDate: currentDate);
        }

        private static MaritalStatus CreateRandomMaritalStatus(
            Random random,
            AgeGroup ageGroup,
            int ageYears)
        {
            if (ageGroup == AgeGroup.Child)
                return MaritalStatus.Single;

            if (ageGroup == AgeGroup.Adult)
            {
                double roll = random.NextDouble();

                if (ageYears < 25)
                    return roll < 0.8
                        ? MaritalStatus.Single
                        : MaritalStatus.Married;

                if (ageYears < 40)
                {
                    if (roll < 0.6)
                        return MaritalStatus.Married;
                    if (roll < 0.8)
                        return MaritalStatus.Single;
                    return MaritalStatus.Divorced;
                }

                if (roll < 0.6)
                    return MaritalStatus.Married;
                if (roll < 0.85)
                    return MaritalStatus.Divorced;
                return MaritalStatus.Single;
            }

            double seniorRoll = random.NextDouble();
            if (seniorRoll < 0.5)
                return MaritalStatus.Married;
            if (seniorRoll < 0.8)
                return MaritalStatus.Widowed;
            return MaritalStatus.Divorced;
        }

        private static Sex CreateRandomSex(Random random)
        {
            return random.Next(
                       minValue: 0,
                       maxValue: 2) ==
                   0
                ? Sex.Male
                : Sex.Female;
        }

        private static PersonName CreateRandomName(
            Random random,
            Sex sex)
        {
            string firstName = sex == Sex.Male
                ? MaleFirstNames[random.Next(MaleFirstNames.Length)]
                : FemaleFirstNames[random.Next(FemaleFirstNames.Length)];

            string lastName = LastNames[random.Next(LastNames.Length)];

            return new PersonName(
                firstName: firstName,
                lastName: lastName);
        }

        private static BodyWeight CreateRandomWeight(
            Random random,
            Sex sex,
            int ageYears)
        {
            decimal kilograms = ageYears switch
            {
                < 1 => random.Next(
                    minValue: 3,
                    maxValue: 11),
                < 3 => random.Next(
                    minValue: 8,
                    maxValue: 16),
                < 7 => random.Next(
                    minValue: 12,
                    maxValue: 26),
                < 13 => random.Next(
                    minValue: 20,
                    maxValue: 46),
                < 18 => sex == Sex.Male
                    ? random.Next(
                        minValue: 40,
                        maxValue: 86)
                    : random.Next(
                        minValue: 38,
                        maxValue: 76),
                < 66 => sex == Sex.Male
                    ? random.Next(
                        minValue: 60,
                        maxValue: 111)
                    : random.Next(
                        minValue: 45,
                        maxValue: 96),
                _ => sex == Sex.Male
                    ? random.Next(
                        minValue: 55,
                        maxValue: 96)
                    : random.Next(
                        minValue: 45,
                        maxValue: 86)
            };

            return BodyWeight.FromKilograms(kilograms);
        }

        private static HealthLevel CreateRandomHealth(
            Random random,
            int ageYears)
        {
            int value = ageYears switch
            {
                < 7 => random.Next(
                    minValue: 70,
                    maxValue: 101),
                < 18 => random.Next(
                    minValue: 60,
                    maxValue: 101),
                < 40 => random.Next(
                    minValue: 50,
                    maxValue: 96),
                < 66 => random.Next(
                    minValue: 45,
                    maxValue: 91),
                < 80 => random.Next(
                    minValue: 35,
                    maxValue: 86),
                _ => random.Next(
                    minValue: 30,
                    maxValue: 81)
            };

            if (ageYears > 40)
            {
                int extraPenalty = Math.Min(
                    val1: 20,
                    val2: (ageYears - 40) / 2);
                value -= random.Next(
                             minValue: 0,
                             maxValue: 6) +
                         extraPenalty;
            }

            value = Math.Clamp(
                value: value,
                min: 0,
                max: 100);
            return HealthLevel.From(value);
        }

        private static int CreateRandomAgeYears(Random random)
        {
            double roll = random.NextDouble();

            if (roll < 0.2)
                return random.Next(
                    minValue: 0,
                    maxValue: 18);

            if (roll < 0.8)
                return random.Next(
                    minValue: 18,
                    maxValue: 66);

            return random.Next(
                minValue: 66,
                maxValue: 121);
        }

        private static HappinessLevel CreateInitialHappiness(
            Random random,
            AgeGroup ageGroup,
            EmploymentStatus employmentStatus)
        {
            int value = random.Next(
                minValue: 40,
                maxValue: 81);

            if (employmentStatus == EmploymentStatus.Employed && ageGroup == AgeGroup.Adult)
                value += random.Next(
                    minValue: 0,
                    maxValue: 11);
            else
                if (employmentStatus == EmploymentStatus.Unemployed && ageGroup == AgeGroup.Adult)
                    value -= random.Next(
                        minValue: 5,
                        maxValue: 16);
                else
                    if (employmentStatus == EmploymentStatus.Retired)
                        value += random.Next(
                            minValue: 0,
                            maxValue: 6);

            value = Math.Clamp(
                value: value,
                min: 0,
                max: 100);
            return HappinessLevel.From(value);
        }

        private static EmploymentStatus CreateRandomEmploymentStatus(
            Random random,
            AgeGroup ageGroup)
        {
            return ageGroup switch
            {
                AgeGroup.Child => random.NextDouble() < 0.9
                    ? EmploymentStatus.Student
                    : EmploymentStatus.Unemployed,
                AgeGroup.Adult => CreateRandomAdultEmploymentStatus(random),
                AgeGroup.Senior => EmploymentStatus.Retired,
                _ => EmploymentStatus.Unemployed
            };
        }

        private static EmploymentStatus CreateRandomAdultEmploymentStatus(Random random)
        {
            double roll = random.NextDouble();

            if (roll < 0.70)
                return EmploymentStatus.Employed;
            if (roll < 0.85)
                return EmploymentStatus.Unemployed;
            return EmploymentStatus.Student;
        }

        private static Job CreateRandomJob(Random random)
        {
            string title = JobTitles[random.Next(JobTitles.Length)];
            var workplaceId = WorkplaceId.New();

            return new Job(
                workplaceId: workplaceId,
                title: title);
        }

        private static EducationLevel CreateRandomEducationLevel(
            Random random,
            int ageYears)
        {
            return ageYears switch
            {
                <= 2 => EducationLevel.None,
                <= 6 => Pick(
                    random: random,
                    (EducationLevel.Preschool, 0.80),
                    (EducationLevel.None, 0.20)),
                <= 10 => Pick(
                    random: random,
                    (EducationLevel.Primary, 0.97),
                    (EducationLevel.LowerSecondary, 0.03)),
                <= 14 => Pick(
                    random: random,
                    (EducationLevel.LowerSecondary, 0.85),
                    (EducationLevel.Primary, 0.15)),
                <= 17 => Pick(
                    random: random,
                    (EducationLevel.UpperSecondary, 0.72),
                    (EducationLevel.LowerSecondary, 0.18),
                    (EducationLevel.Vocational, 0.10)),
                <= 21 => Pick(
                    random: random,
                    (EducationLevel.UpperSecondary, 0.25),
                    (EducationLevel.Vocational, 0.33),
                    (EducationLevel.Higher, 0.40),
                    (EducationLevel.LowerSecondary, 0.02)),
                <= 65 => Pick(
                    random: random,
                    (EducationLevel.None, 0.01),
                    (EducationLevel.Primary, 0.08),
                    (EducationLevel.LowerSecondary, 0.22),
                    (EducationLevel.UpperSecondary, 0.30),
                    (EducationLevel.Vocational, 0.22),
                    (EducationLevel.Higher, 0.15),
                    (EducationLevel.Postgraduate, 0.02)),
                _ => Pick(
                    random: random,
                    (EducationLevel.None, 0.02),
                    (EducationLevel.Primary, 0.18),
                    (EducationLevel.LowerSecondary, 0.34),
                    (EducationLevel.UpperSecondary, 0.25),
                    (EducationLevel.Vocational, 0.12),
                    (EducationLevel.Higher, 0.08),
                    (EducationLevel.Postgraduate, 0.01))
            };
        }

        private static EducationLevel Pick(
            Random random,
            params (EducationLevel level, double weight)[] items)
        {
            double total = 0;
            for (int i = 0; i < items.Length; i++)
                total += items[i].weight;

            double roll = random.NextDouble() * total;
            double accumulated = 0;

            for (int i = 0; i < items.Length; i++)
            {
                accumulated += items[i].weight;
                if (roll < accumulated)
                    return items[i].level;
            }

            return items[^1].level;
        }

        private sealed class BuildingCapacityState(
            ResidentialBuildingId buildingId,
            DistrictId districtId,
            int remainingCapacity)
        {
            public ResidentialBuildingId BuildingId { get; } = buildingId;
            public DistrictId DistrictId { get; } = districtId;
            public int RemainingCapacity { get; set; } = remainingCapacity;
        }
    }
}
