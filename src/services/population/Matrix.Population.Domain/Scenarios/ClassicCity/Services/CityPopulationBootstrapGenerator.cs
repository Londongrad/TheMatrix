using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.Scenarios.ClassicCity.Entities;
using Matrix.Population.Domain.Scenarios.ClassicCity.Enums;
using Matrix.Population.Domain.Scenarios.ClassicCity.Models;
using Matrix.Population.Domain.Scenarios.ClassicCity.Services.Abstractions;
using Matrix.Population.Domain.Scenarios.ClassicCity.ValueObjects;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Scenarios.ClassicCity.Services
{
    public sealed class CityPopulationBootstrapGenerator(IPopulationGenerationContentCatalog contentCatalog)
    {
        private readonly IPopulationGenerationContentCatalog _contentCatalog = contentCatalog;

        public PopulationBootstrapResult GenerateStandalone(
            int peopleCount,
            DateOnly currentDate,
            DateTimeOffset createdAtUtc,
            int? randomSeed = null)
        {
            if (peopleCount <= 0)
                return new PopulationBootstrapResult(
                    Households: Array.Empty<Household>(),
                    HouseholdPlacements: Array.Empty<ClassicCityHouseholdPlacement>(),
                    Persons: Array.Empty<Person>());

            Random random = CreateRandom(randomSeed);
            var households = new List<Household>(peopleCount);
            var householdPlacements = new List<ClassicCityHouseholdPlacement>();
            var persons = new List<Person>(peopleCount);

            for (int i = 0; i < peopleCount; i++)
            {
                var householdId = HouseholdId.New();
                var household = Household.Create(
                    id: householdId,
                    size: HouseholdSize.From(1),
                    createdAtUtc: createdAtUtc);

                households.Add(household);
                persons.Add(
                    CreateSingleResident(
                        random: random,
                        householdId: householdId,
                        currentDate: currentDate,
                        tuning: BootstrapTuningModel.Default(),
                        housingStatus: HousingStatus.Housed));
            }

            return new PopulationBootstrapResult(
                Households: households,
                HouseholdPlacements: householdPlacements,
                Persons: persons);
        }

        public PopulationBootstrapResult GenerateForCity(
            CityId cityId,
            IReadOnlyCollection<ResidentialBuildingResidence> residentialBuildings,
            int peopleCount,
            DateOnly currentDate,
            DateTimeOffset createdAtUtc,
            CityPopulationBootstrapTuning tuning,
            int? randomSeed = null)
        {
            if (peopleCount <= 0)
                return new PopulationBootstrapResult(
                    Households: Array.Empty<Household>(),
                    HouseholdPlacements: Array.Empty<ClassicCityHouseholdPlacement>(),
                    Persons: Array.Empty<Person>());

            Random random = CreateRandom(randomSeed);
            BootstrapTuningModel bootstrapTuning = BootstrapTuningModel.From(tuning);
            var households = new List<Household>();
            var householdPlacements = new List<ClassicCityHouseholdPlacement>();
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
                    remainingPeople: remainingPeople,
                    tuning: bootstrapTuning);
                var householdSize = HouseholdSize.From(householdSizeValue);
                var householdId = HouseholdId.New();
                Household household = Household.Create(
                    id: householdId,
                    size: householdSize,
                    createdAtUtc: createdAtUtc);
                ClassicCityHouseholdPlacement householdPlacement = TryAllocateHouseholdPlacement(
                    cityId: cityId,
                    householdId: householdId,
                    householdSize: householdSize,
                    capacityStates: capacityStates,
                    random: random);

                households.Add(household);
                householdPlacements.Add(householdPlacement);
                persons.AddRange(
                    CreateHouseholdMembers(
                        random: random,
                        householdId: household.Id,
                        householdSizeValue: householdSizeValue,
                        currentDate: currentDate,
                        tuning: bootstrapTuning,
                        housingStatus: householdPlacement.HousingStatus));

                remainingPeople -= householdSizeValue;
            }

            return new PopulationBootstrapResult(
                Households: households,
                HouseholdPlacements: householdPlacements,
                Persons: persons);
        }

        private IReadOnlyCollection<Person> CreateHouseholdMembers(
            Random random,
            HouseholdId householdId,
            int householdSizeValue,
            DateOnly currentDate,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
        {
            if (householdSizeValue <= 0)
                return Array.Empty<Person>();

            if (householdSizeValue == 1)
                return new[]
                {
                    CreateSingleResident(
                        random: random,
                        householdId: householdId,
                        currentDate: currentDate,
                        tuning: tuning,
                        housingStatus: housingStatus)
                };

            HouseholdComposition composition = PickHouseholdComposition(
                random: random,
                householdSizeValue: householdSizeValue,
                tuning: tuning);

            return composition switch
            {
                HouseholdComposition.MarriedFamily => CreateMarriedFamily(
                    random: random,
                    householdId: householdId,
                    householdSizeValue: householdSizeValue,
                    currentDate: currentDate,
                    tuning: tuning,
                    housingStatus: housingStatus),
                HouseholdComposition.SingleParentFamily => CreateSingleParentFamily(
                    random: random,
                    householdId: householdId,
                    householdSizeValue: householdSizeValue,
                    currentDate: currentDate,
                    tuning: tuning,
                    housingStatus: housingStatus),
                _ => CreateAdultOnlyHousehold(
                    random: random,
                    householdId: householdId,
                    householdSizeValue: householdSizeValue,
                    currentDate: currentDate,
                    tuning: tuning,
                    housingStatus: housingStatus)
            };
        }

        private static HouseholdComposition PickHouseholdComposition(
            Random random,
            int householdSizeValue,
            BootstrapTuningModel tuning)
        {
            double familyFactor = tuning.FamilyFormationFactor;
            double pressureFactor = tuning.HousingPressureFactor;
            double volatilityFactor = tuning.SocialVolatilityFactor;

            return householdSizeValue == 2
                ? PickWeighted(
                    random: random,
                    (HouseholdComposition.MarriedFamily, Math.Max(0.10, 2.60 + (familyFactor * 1.40) - (pressureFactor * 0.80))),
                    (HouseholdComposition.SingleParentFamily, Math.Max(0.10, 0.80 + (pressureFactor * 0.70) + (volatilityFactor * 0.30))),
                    (HouseholdComposition.AdultOnly, Math.Max(0.10, 0.90 + (pressureFactor * 1.00) + (volatilityFactor * 0.25) - (familyFactor * 0.35))))
                : PickWeighted(
                    random: random,
                    (HouseholdComposition.MarriedFamily, Math.Max(0.10, 2.20 + (familyFactor * 1.80) - (pressureFactor * 0.60))),
                    (HouseholdComposition.SingleParentFamily, Math.Max(0.10, 1.00 + (pressureFactor * 0.90) + (volatilityFactor * 0.50))),
                    (HouseholdComposition.AdultOnly, Math.Max(0.10, 1.10 + (pressureFactor * 1.10) + (volatilityFactor * 0.35) - (familyFactor * 0.45))));
        }

        private IReadOnlyCollection<Person> CreateMarriedFamily(
            Random random,
            HouseholdId householdId,
            int householdSizeValue,
            DateOnly currentDate,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
        {
            var persons = new List<Person>(householdSizeValue);

            PopulationFamilySurnameCatalogItem familySurname = CreateRandomFamilySurname(random);
            var firstSpouseId = PersonId.New();
            var secondSpouseId = PersonId.New();

            Sex firstSpouseSex = CreateRandomSex(random);
            Sex secondSpouseSex = CreateOppositeSex(firstSpouseSex);
            int firstSpouseAgeYears = CreateRandomParentAgeYears(random);
            int secondSpouseAgeYears = CreateRandomSpouseAgeYears(
                random: random,
                primarySpouseAgeYears: firstSpouseAgeYears);

            persons.Add(
                CreateGeneratedPerson(
                    random: random,
                    personId: firstSpouseId,
                    householdId: householdId,
                    currentDate: currentDate,
                    sex: firstSpouseSex,
                    ageYears: firstSpouseAgeYears,
                    maritalStatus: MaritalStatus.Married,
                    spouseId: secondSpouseId,
                    familySurname: familySurname,
                    tuning: tuning,
                    housingStatus: housingStatus));

            persons.Add(
                CreateGeneratedPerson(
                    random: random,
                    personId: secondSpouseId,
                    householdId: householdId,
                    currentDate: currentDate,
                    sex: secondSpouseSex,
                    ageYears: secondSpouseAgeYears,
                    maritalStatus: MaritalStatus.Married,
                    spouseId: firstSpouseId,
                    familySurname: familySurname,
                    tuning: tuning,
                    housingStatus: housingStatus));

            int remainingMembers = householdSizeValue - 2;
            int youngestParentAgeYears = Math.Min(
                val1: firstSpouseAgeYears,
                val2: secondSpouseAgeYears);
            int childCount = DetermineChildCountForPartneredFamily(
                random: random,
                remainingMembers: remainingMembers,
                youngestParentAgeYears: youngestParentAgeYears);

            AddChildren(
                persons: persons,
                random: random,
                householdId: householdId,
                currentDate: currentDate,
                childCount: childCount,
                youngestCaregiverAgeYears: youngestParentAgeYears,
                familySurname: familySurname,
                tuning: tuning,
                housingStatus: housingStatus);

            AddAdultRelatives(
                persons: persons,
                random: random,
                householdId: householdId,
                currentDate: currentDate,
                count: remainingMembers - childCount,
                familySurname: familySurname,
                tuning: tuning,
                housingStatus: housingStatus);

            return persons;
        }

        private IReadOnlyCollection<Person> CreateSingleParentFamily(
            Random random,
            HouseholdId householdId,
            int householdSizeValue,
            DateOnly currentDate,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
        {
            var persons = new List<Person>(householdSizeValue);

            PopulationFamilySurnameCatalogItem familySurname = CreateRandomFamilySurname(random);
            int parentAgeYears = CreateRandomParentAgeYears(random);
            MaritalStatus parentMaritalStatus = CreateSingleParentMaritalStatus(
                random: random,
                ageYears: parentAgeYears);

            persons.Add(
                CreateGeneratedPerson(
                    random: random,
                    personId: PersonId.New(),
                    householdId: householdId,
                    currentDate: currentDate,
                    sex: CreateRandomSex(random),
                    ageYears: parentAgeYears,
                    maritalStatus: parentMaritalStatus,
                    spouseId: null,
                    familySurname: familySurname,
                    tuning: tuning,
                    housingStatus: housingStatus));

            int remainingMembers = householdSizeValue - 1;
            int childCount = DetermineChildCountForSingleParentFamily(
                remainingMembers: remainingMembers,
                parentAgeYears: parentAgeYears);

            AddChildren(
                persons: persons,
                random: random,
                householdId: householdId,
                currentDate: currentDate,
                childCount: childCount,
                youngestCaregiverAgeYears: parentAgeYears,
                familySurname: familySurname,
                tuning: tuning,
                housingStatus: housingStatus);

            AddAdultRelatives(
                persons: persons,
                random: random,
                householdId: householdId,
                currentDate: currentDate,
                count: remainingMembers - childCount,
                familySurname: familySurname,
                tuning: tuning,
                housingStatus: housingStatus);

            return persons;
        }

        private IReadOnlyCollection<Person> CreateAdultOnlyHousehold(
            Random random,
            HouseholdId householdId,
            int householdSizeValue,
            DateOnly currentDate,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
        {
            var persons = new List<Person>(householdSizeValue);

            PopulationFamilySurnameCatalogItem? sharedFamilySurname = random.NextDouble() < 0.35
                ? CreateRandomFamilySurname(random)
                : null;

            for (int i = 0; i < householdSizeValue; i++)
            {
                int ageYears = CreateRandomIndependentAdultAgeYears(random);
                persons.Add(
                    CreateGeneratedPerson(
                        random: random,
                        personId: PersonId.New(),
                        householdId: householdId,
                        currentDate: currentDate,
                        sex: CreateRandomSex(random),
                        ageYears: ageYears,
                        maritalStatus: CreateRandomNonMarriedAdultStatus(
                            random: random,
                            ageYears: ageYears),
                        spouseId: null,
                        familySurname: sharedFamilySurname,
                        tuning: tuning,
                        housingStatus: housingStatus));
            }

            return persons;
        }

        private void AddChildren(
            List<Person> persons,
            Random random,
            HouseholdId householdId,
            DateOnly currentDate,
            int childCount,
            int youngestCaregiverAgeYears,
            PopulationFamilySurnameCatalogItem familySurname,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
        {
            for (int i = 0; i < childCount; i++)
                persons.Add(
                    CreateGeneratedPerson(
                        random: random,
                        personId: PersonId.New(),
                        householdId: householdId,
                        currentDate: currentDate,
                        sex: CreateRandomSex(random),
                        ageYears: CreateRandomChildAgeYears(
                            random: random,
                            youngestCaregiverAgeYears: youngestCaregiverAgeYears),
                        maritalStatus: MaritalStatus.Single,
                        spouseId: null,
                        familySurname: familySurname,
                        tuning: tuning,
                        housingStatus: housingStatus));
        }

        private void AddAdultRelatives(
            List<Person> persons,
            Random random,
            HouseholdId householdId,
            DateOnly currentDate,
            int count,
            PopulationFamilySurnameCatalogItem familySurname,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
        {
            for (int i = 0; i < count; i++)
            {
                int ageYears = CreateRandomIndependentAdultAgeYears(random);
                persons.Add(
                    CreateGeneratedPerson(
                        random: random,
                        personId: PersonId.New(),
                        householdId: householdId,
                        currentDate: currentDate,
                        sex: CreateRandomSex(random),
                        ageYears: ageYears,
                        maritalStatus: CreateRandomNonMarriedAdultStatus(
                            random: random,
                            ageYears: ageYears),
                        spouseId: null,
                        familySurname: familySurname,
                        tuning: tuning,
                        housingStatus: housingStatus));
            }
        }

        private Person CreateSingleResident(
            Random random,
            HouseholdId householdId,
            DateOnly currentDate,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
        {
            int ageYears = CreateRandomIndependentAdultAgeYears(random);
            PopulationFamilySurnameCatalogItem familySurname = CreateRandomFamilySurname(random);

            return CreateGeneratedPerson(
                random: random,
                personId: PersonId.New(),
                householdId: householdId,
                currentDate: currentDate,
                sex: CreateRandomSex(random),
                ageYears: ageYears,
                maritalStatus: CreateRandomNonMarriedAdultStatus(
                    random: random,
                    ageYears: ageYears),
                spouseId: null,
                familySurname: familySurname,
                tuning: tuning,
                housingStatus: housingStatus);
        }

        private Person CreateGeneratedPerson(
            Random random,
            PersonId personId,
            HouseholdId householdId,
            DateOnly currentDate,
            Sex sex,
            int ageYears,
            MaritalStatus maritalStatus,
            PersonId? spouseId,
            PopulationFamilySurnameCatalogItem? familySurname,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
        {
            PersonName name = CreateRandomName(
                random: random,
                sex: sex,
                familySurname: familySurname);
            DateOnly birthDate = currentDate.AddYears(-ageYears);
            var age = Age.FromYears(ageYears);
            AgeGroup ageGroup = AgeGroupRules.GetAgeGroup(age);
            PersonalityArchetype personalityArchetype = CreateRandomPersonalityArchetype(
                random: random,
                tuning: tuning,
                housingStatus: housingStatus,
                maritalStatus: maritalStatus);
            var personality = Personality.CreateRandom(
                random: random,
                archetype: personalityArchetype);
            HealthLevel health = CreateRandomHealth(
                random: random,
                ageYears: ageYears,
                tuning: tuning,
                housingStatus: housingStatus);
            BodyWeight weight = CreateRandomWeight(
                random: random,
                sex: sex,
                ageYears: ageYears);
            EmploymentStatus employmentStatus = CreateRandomEmploymentStatus(
                random: random,
                ageGroup: ageGroup,
                tuning: tuning);
            Job? job = employmentStatus == EmploymentStatus.Employed
                ? CreateRandomJob(random)
                : null;
            HappinessLevel happiness = CreateInitialHappiness(
                random: random,
                ageGroup: ageGroup,
                employmentStatus: employmentStatus,
                maritalStatus: maritalStatus,
                personality: personality,
                tuning: tuning,
                housingStatus: housingStatus);
            EnergyLevel energy = CreateInitialEnergy(
                random: random,
                ageGroup: ageGroup,
                employmentStatus: employmentStatus,
                personality: personality,
                tuning: tuning,
                housingStatus: housingStatus);
            StressLevel stress = CreateInitialStress(
                random: random,
                ageGroup: ageGroup,
                employmentStatus: employmentStatus,
                personality: personality,
                tuning: tuning,
                housingStatus: housingStatus);
            SocialNeedLevel socialNeed = CreateInitialSocialNeed(
                random: random,
                ageGroup: ageGroup,
                employmentStatus: employmentStatus,
                maritalStatus: maritalStatus,
                personality: personality,
                tuning: tuning,
                housingStatus: housingStatus);
            EducationLevel educationLevel = CreateRandomEducationLevel(
                random: random,
                ageYears: ageYears);

            return Person.CreatePerson(
                id: personId,
                householdId: householdId,
                name: name,
                sex: sex,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: maritalStatus,
                spouseId: spouseId,
                educationLevel: educationLevel,
                employmentStatus: employmentStatus,
                happinessLevel: happiness,
                energyLevel: energy,
                stressLevel: stress,
                socialNeedLevel: socialNeed,
                personality: personality,
                birthDate: birthDate,
                healthLevel: health,
                weight: weight,
                job: job,
                currentDate: currentDate);
        }

        private static ClassicCityHouseholdPlacement TryAllocateHouseholdPlacement(
            CityId cityId,
            HouseholdId householdId,
            HouseholdSize householdSize,
            List<BuildingCapacityState> capacityStates,
            Random random)
        {
            var candidates = capacityStates
               .Where(x => x.RemainingCapacity >= householdSize.Value)
               .ToList();

            if (candidates.Count == 0)
                return ClassicCityHouseholdPlacement.CreateHomeless(
                    householdId: householdId,
                    cityId: cityId);

            BuildingCapacityState selected = candidates[random.Next(candidates.Count)];
            selected.RemainingCapacity -= householdSize.Value;

            return ClassicCityHouseholdPlacement.CreateHoused(
                householdId: householdId,
                cityId: cityId,
                districtId: selected.DistrictId,
                residentialBuildingId: selected.BuildingId);
        }

        private static int NextHouseholdSize(
            Random random,
            int remainingPeople,
            BootstrapTuningModel tuning)
        {
            double pressureFactor = tuning.HousingPressureFactor;
            double familyFactor = tuning.FamilyFormationFactor;
            double volatilityFactor = tuning.SocialVolatilityFactor;

            int selected = PickWeighted(
                random: random,
                (1, Math.Max(0.10, 1.40 + (pressureFactor * 1.50) - (familyFactor * 0.60) + (volatilityFactor * 0.20))),
                (2, Math.Max(0.10, 2.30 + (pressureFactor * 0.40) + (familyFactor * 0.20))),
                (3, Math.Max(0.10, 1.70 + (familyFactor * 1.00) - (pressureFactor * 0.40))),
                (4, Math.Max(0.10, 1.00 + (familyFactor * 1.20) - (pressureFactor * 0.70))),
                (5, Math.Max(0.10, 0.35 + (familyFactor * 0.90) - (pressureFactor * 0.80))));
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

        private static int DetermineChildCountForPartneredFamily(
            Random random,
            int remainingMembers,
            int youngestParentAgeYears)
        {
            if (remainingMembers <= 0)
                return 0;

            int maxChildAgeYears = Math.Min(
                val1: 17,
                val2: youngestParentAgeYears - 18);
            if (maxChildAgeYears < 0)
                return 0;

            double probability = youngestParentAgeYears switch
            {
                < 28 => 0.85,
                < 42 => 0.75,
                < 55 => 0.55,
                _ => 0.25
            };

            if (random.NextDouble() >= probability)
                return 0;

            int minimum = remainingMembers == 1
                ? 1
                : 0;
            return random.Next(
                minValue: minimum,
                maxValue: remainingMembers + 1);
        }

        private static int DetermineChildCountForSingleParentFamily(
            int remainingMembers,
            int parentAgeYears)
        {
            if (remainingMembers <= 0)
                return 0;

            int maxChildAgeYears = Math.Min(
                val1: 17,
                val2: parentAgeYears - 18);
            if (maxChildAgeYears < 0)
                return 0;

            return remainingMembers;
        }

        private static int CreateRandomParentAgeYears(Random random)
        {
            double roll = random.NextDouble();

            if (roll < 0.20)
                return random.Next(
                    minValue: 20,
                    maxValue: 28);
            if (roll < 0.75)
                return random.Next(
                    minValue: 28,
                    maxValue: 46);
            if (roll < 0.95)
                return random.Next(
                    minValue: 46,
                    maxValue: 61);
            return random.Next(
                minValue: 61,
                maxValue: 76);
        }

        private static int CreateRandomSpouseAgeYears(
            Random random,
            int primarySpouseAgeYears)
        {
            int delta = random.Next(
                minValue: -8,
                maxValue: 9);
            return Math.Clamp(
                value: primarySpouseAgeYears + delta,
                min: 18,
                max: 85);
        }

        private static int CreateRandomChildAgeYears(
            Random random,
            int youngestCaregiverAgeYears)
        {
            int maxChildAgeYears = Math.Min(
                val1: 17,
                val2: youngestCaregiverAgeYears - 18);
            int upperBoundExclusive = Math.Max(
                val1: 1,
                val2: maxChildAgeYears + 1);
            return random.Next(
                minValue: 0,
                maxValue: upperBoundExclusive);
        }

        private static int CreateRandomIndependentAdultAgeYears(Random random)
        {
            double roll = random.NextDouble();

            if (roll < 0.70)
                return random.Next(
                    minValue: 18,
                    maxValue: 66);
            return random.Next(
                minValue: 66,
                maxValue: 91);
        }

        private static MaritalStatus CreateRandomNonMarriedAdultStatus(
            Random random,
            int ageYears)
        {
            if (ageYears < 22)
                return MaritalStatus.Single;

            if (ageYears < 40)
                return random.NextDouble() < 0.75
                    ? MaritalStatus.Single
                    : MaritalStatus.Divorced;

            if (ageYears < 66)
            {
                double roll = random.NextDouble();
                if (roll < 0.45)
                    return MaritalStatus.Single;
                if (roll < 0.85)
                    return MaritalStatus.Divorced;
                return MaritalStatus.Widowed;
            }

            double seniorRoll = random.NextDouble();
            if (seniorRoll < 0.55)
                return MaritalStatus.Widowed;
            if (seniorRoll < 0.80)
                return MaritalStatus.Divorced;
            return MaritalStatus.Single;
        }

        private static MaritalStatus CreateSingleParentMaritalStatus(
            Random random,
            int ageYears)
        {
            if (ageYears < 26)
                return MaritalStatus.Single;

            double roll = random.NextDouble();
            if (ageYears < 40)
                return roll < 0.55
                    ? MaritalStatus.Single
                    : MaritalStatus.Divorced;

            if (ageYears < 66)
            {
                if (roll < 0.35)
                    return MaritalStatus.Single;
                if (roll < 0.80)
                    return MaritalStatus.Divorced;
                return MaritalStatus.Widowed;
            }

            return roll < 0.65
                ? MaritalStatus.Widowed
                : MaritalStatus.Divorced;
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

        private static Sex CreateOppositeSex(Sex sex)
        {
            return sex == Sex.Male
                ? Sex.Female
                : Sex.Male;
        }

        private PersonName CreateRandomName(
            Random random,
            Sex sex,
            PopulationFamilySurnameCatalogItem? familySurname)
        {
            string firstName = sex == Sex.Male
                ? _contentCatalog.MaleFirstNames[random.Next(_contentCatalog.MaleFirstNames.Count)]
                : _contentCatalog.FemaleFirstNames[random.Next(_contentCatalog.FemaleFirstNames.Count)];

            PopulationFamilySurnameCatalogItem resolvedSurname = familySurname ?? CreateRandomFamilySurname(random);
            string lastName = ResolveSurnameForSex(
                surname: resolvedSurname,
                sex: sex);

            return new PersonName(
                firstName: firstName,
                lastName: lastName);
        }

        private PopulationFamilySurnameCatalogItem CreateRandomFamilySurname(Random random)
        {
            return _contentCatalog.FamilySurnames[random.Next(_contentCatalog.FamilySurnames.Count)];
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
            int ageYears,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
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

            value += ScaleFromNeutral(
                percent: tuning.EconomicStabilityPercent,
                maxMagnitude: 10);
            value -= ScaleFromNeutral(
                percent: tuning.HousingPressurePercent,
                maxMagnitude: 8);

            if (housingStatus == HousingStatus.Homeless)
                value -= random.Next(
                    minValue: 6,
                    maxValue: 16);

            value += GetVolatilityJitter(
                random: random,
                tuning: tuning,
                maxAbsMagnitude: 6);

            value = Math.Clamp(
                value: value,
                min: 0,
                max: 100);
            return HealthLevel.From(value);
        }

        private static HappinessLevel CreateInitialHappiness(
            Random random,
            AgeGroup ageGroup,
            EmploymentStatus employmentStatus,
            MaritalStatus maritalStatus,
            Personality personality,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
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

            value += maritalStatus switch
            {
                MaritalStatus.Married => random.Next(
                    minValue: 3,
                    maxValue: 9),
                MaritalStatus.Widowed => -random.Next(
                    minValue: 4,
                    maxValue: 10),
                MaritalStatus.Divorced => -random.Next(
                    minValue: 2,
                    maxValue: 8),
                _ => 0
            };

            value += ScaleFromNeutral(
                percent: tuning.EconomicStabilityPercent,
                maxMagnitude: 12);
            value -= ScaleFromNeutral(
                percent: tuning.HousingPressurePercent,
                maxMagnitude: 10);
            value += ScaleFromNeutral(
                percent: personality.Optimism,
                maxMagnitude: 10);
            value += ScaleFromNeutral(
                percent: personality.Sociability,
                maxMagnitude: 4);

            if (housingStatus == HousingStatus.Homeless)
                value -= random.Next(
                    minValue: 12,
                    maxValue: 25);

            value += GetVolatilityJitter(
                random: random,
                tuning: tuning,
                maxAbsMagnitude: 8);

            value = Math.Clamp(
                value: value,
                min: 0,
                max: 100);
            return HappinessLevel.From(value);
        }

        private static EmploymentStatus CreateRandomEmploymentStatus(
            Random random,
            AgeGroup ageGroup,
            BootstrapTuningModel tuning)
        {
            return ageGroup switch
            {
                AgeGroup.Child => PickWeighted(
                    random: random,
                    (EmploymentStatus.Student, Math.Max(0.10, 6.00 + (tuning.EconomicStabilityFactor * 0.60) - (tuning.HousingPressureFactor * 0.40))),
                    (EmploymentStatus.Unemployed, Math.Max(0.10, 0.20 + (tuning.HousingPressureFactor * 0.50)))),
                AgeGroup.Youth => PickWeighted(
                    random: random,
                    (EmploymentStatus.Student, Math.Max(0.10, 4.60 + (tuning.EconomicStabilityFactor * 1.00) - (tuning.HousingPressureFactor * 0.80))),
                    (EmploymentStatus.Unemployed, Math.Max(0.10, 0.60 + (tuning.HousingPressureFactor * 1.10) + (tuning.SocialVolatilityFactor * 0.20)))),
                AgeGroup.Adult => CreateRandomAdultEmploymentStatus(
                    random: random,
                    tuning: tuning),
                AgeGroup.Senior => EmploymentStatus.Retired,
                _ => EmploymentStatus.Unemployed
            };
        }

        private static EmploymentStatus CreateRandomAdultEmploymentStatus(
            Random random,
            BootstrapTuningModel tuning)
        {
            return PickWeighted(
                random: random,
                (EmploymentStatus.Employed, Math.Max(0.10, 4.80 + (tuning.EconomicStabilityFactor * 2.20) - (tuning.HousingPressureFactor * 1.60))),
                (EmploymentStatus.Unemployed, Math.Max(0.10, 1.60 + (tuning.HousingPressureFactor * 2.10) - (tuning.EconomicStabilityFactor * 1.10))),
                (EmploymentStatus.Student, Math.Max(0.10, 0.70 + (tuning.EconomicStabilityFactor * 0.60) + (tuning.SocialVolatilityFactor * 0.40))));
        }

        private static EnergyLevel CreateInitialEnergy(
            Random random,
            AgeGroup ageGroup,
            EmploymentStatus employmentStatus,
            Personality personality,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
        {
            int baseValue = ageGroup switch
            {
                AgeGroup.Child => random.Next(
                    minValue: 78,
                    maxValue: 101),
                AgeGroup.Youth => random.Next(
                    minValue: 70,
                    maxValue: 96),
                AgeGroup.Adult => random.Next(
                    minValue: 58,
                    maxValue: 86),
                AgeGroup.Senior => random.Next(
                    minValue: 48,
                    maxValue: 76),
                _ => random.Next(
                    minValue: 60,
                    maxValue: 81)
            };

            baseValue += employmentStatus switch
            {
                EmploymentStatus.Employed => -5,
                EmploymentStatus.Student => -3,
                EmploymentStatus.Retired => +4,
                _ => 0
            };

            baseValue += ScaleFromNeutral(
                percent: tuning.EconomicStabilityPercent,
                maxMagnitude: 8);
            baseValue -= ScaleFromNeutral(
                percent: tuning.HousingPressurePercent,
                maxMagnitude: 6);
            baseValue += ScaleFromNeutral(
                percent: personality.Discipline,
                maxMagnitude: 6);

            if (housingStatus == HousingStatus.Homeless)
                baseValue -= random.Next(
                    minValue: 10,
                    maxValue: 21);

            baseValue += GetVolatilityJitter(
                random: random,
                tuning: tuning,
                maxAbsMagnitude: 5);

            return EnergyLevel.From(
                Math.Clamp(
                    value: baseValue,
                    min: EnergyLevel.MinEnergy,
                    max: EnergyLevel.MaxEnergy));
        }

        private static StressLevel CreateInitialStress(
            Random random,
            AgeGroup ageGroup,
            EmploymentStatus employmentStatus,
            Personality personality,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
        {
            int baseValue = ageGroup switch
            {
                AgeGroup.Child => random.Next(
                    minValue: 5,
                    maxValue: 31),
                AgeGroup.Youth => random.Next(
                    minValue: 15,
                    maxValue: 46),
                AgeGroup.Adult => random.Next(
                    minValue: 20,
                    maxValue: 56),
                AgeGroup.Senior => random.Next(
                    minValue: 10,
                    maxValue: 36),
                _ => random.Next(
                    minValue: 15,
                    maxValue: 41)
            };

            baseValue += employmentStatus switch
            {
                EmploymentStatus.Employed => +8,
                EmploymentStatus.Student => +6,
                EmploymentStatus.Unemployed => +3,
                EmploymentStatus.Retired => -4,
                _ => 0
            };

            baseValue += ScaleFromNeutral(
                percent: tuning.HousingPressurePercent,
                maxMagnitude: 14);
            baseValue -= ScaleFromNeutral(
                percent: tuning.EconomicStabilityPercent,
                maxMagnitude: 8);
            baseValue += ScaleFromNeutral(
                percent: tuning.SocialVolatilityPercent,
                maxMagnitude: 10);
            baseValue -= ScaleFromNeutral(
                percent: personality.Optimism,
                maxMagnitude: 6);
            baseValue -= ScaleFromNeutral(
                percent: personality.Discipline,
                maxMagnitude: 4);

            if (housingStatus == HousingStatus.Homeless)
                baseValue += random.Next(
                    minValue: 18,
                    maxValue: 31);

            return StressLevel.From(
                Math.Clamp(
                    value: baseValue,
                    min: StressLevel.MinStress,
                    max: StressLevel.MaxStress));
        }

        private static SocialNeedLevel CreateInitialSocialNeed(
            Random random,
            AgeGroup ageGroup,
            EmploymentStatus employmentStatus,
            MaritalStatus maritalStatus,
            Personality personality,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus)
        {
            int baseValue = ageGroup switch
            {
                AgeGroup.Child => random.Next(
                    minValue: 10,
                    maxValue: 36),
                AgeGroup.Youth => random.Next(
                    minValue: 15,
                    maxValue: 41),
                AgeGroup.Adult => random.Next(
                    minValue: 30,
                    maxValue: 61),
                AgeGroup.Senior => random.Next(
                    minValue: 25,
                    maxValue: 56),
                _ => random.Next(
                    minValue: 20,
                    maxValue: 51)
            };

            baseValue += employmentStatus switch
            {
                EmploymentStatus.Unemployed => +6,
                EmploymentStatus.Retired => +4,
                EmploymentStatus.Employed => -3,
                EmploymentStatus.Student => -2,
                _ => 0
            };

            baseValue += maritalStatus switch
            {
                MaritalStatus.Married => -15,
                MaritalStatus.Divorced => +8,
                MaritalStatus.Widowed => +12,
                _ => 0
            };

            baseValue += ScaleFromNeutral(
                percent: personality.Sociability,
                maxMagnitude: 12);
            baseValue += ScaleFromNeutral(
                percent: tuning.SocialVolatilityPercent,
                maxMagnitude: 6);
            baseValue += ScaleFromNeutral(
                percent: tuning.HousingPressurePercent,
                maxMagnitude: 5);
            baseValue -= ScaleFromNeutral(
                percent: tuning.FamilyFormationPercent,
                maxMagnitude: 4);

            if (housingStatus == HousingStatus.Homeless)
                baseValue += random.Next(
                    minValue: 8,
                    maxValue: 18);

            baseValue += GetVolatilityJitter(
                random: random,
                tuning: tuning,
                maxAbsMagnitude: 4);

            return SocialNeedLevel.From(
                Math.Clamp(
                    value: baseValue,
                    min: SocialNeedLevel.MinSocialNeed,
                    max: SocialNeedLevel.MaxSocialNeed));
        }

        private Job CreateRandomJob(Random random)
        {
            PopulationProfessionCatalogItem profession = PickProfession(random);
            var workplaceId = WorkplaceId.New();

            return new Job(
                workplaceId: workplaceId,
                title: profession.Title);
        }

        private PopulationProfessionCatalogItem PickProfession(Random random)
        {
            int totalWeight = 0;
            for (int i = 0; i < _contentCatalog.Professions.Count; i++)
                totalWeight += _contentCatalog.Professions[i].Weight;

            int roll = random.Next(
                minValue: 0,
                maxValue: totalWeight);
            int accumulated = 0;

            for (int i = 0; i < _contentCatalog.Professions.Count; i++)
            {
                PopulationProfessionCatalogItem item = _contentCatalog.Professions[i];
                accumulated += item.Weight;
                if (roll < accumulated)
                    return item;
            }

            return _contentCatalog.Professions[^1];
        }

        private static string ResolveSurnameForSex(
            PopulationFamilySurnameCatalogItem surname,
            Sex sex)
        {
            return sex == Sex.Female
                ? surname.Feminine
                : surname.Masculine;
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

        private static PersonalityArchetype CreateRandomPersonalityArchetype(
            Random random,
            BootstrapTuningModel tuning,
            HousingStatus housingStatus,
            MaritalStatus maritalStatus)
        {
            double homelessBias = housingStatus == HousingStatus.Homeless
                ? 1.0
                : 0.0;
            double housedBias = housingStatus == HousingStatus.Housed
                ? 0.6
                : 0.0;
            double marriedBias = maritalStatus == MaritalStatus.Married
                ? 0.4
                : 0.0;

            return PickWeighted(
                random: random,
                (PersonalityArchetype.Balanced, Math.Max(0.10, 2.80 - (tuning.SocialVolatilityFactor * 0.80))),
                (PersonalityArchetype.Optimist, Math.Max(0.10, 1.10 + (tuning.EconomicStabilityFactor * 1.40) + housedBias + marriedBias - (tuning.HousingPressureFactor * 0.60))),
                (PersonalityArchetype.Pessimist, Math.Max(0.10, 0.70 + (tuning.HousingPressureFactor * 1.60) + (tuning.SocialVolatilityFactor * 0.50) + homelessBias)),
                (PersonalityArchetype.RiskTaker, Math.Max(0.10, 0.80 + (tuning.SocialVolatilityFactor * 1.30) - (tuning.HousingPressureFactor * 0.20))),
                (PersonalityArchetype.Cautious, Math.Max(0.10, 0.90 + (tuning.HousingPressureFactor * 1.40) + (homelessBias * 0.80))),
                (PersonalityArchetype.Workaholic, Math.Max(0.10, 0.80 + (tuning.EconomicStabilityFactor * 0.80) + (tuning.HousingPressureFactor * 0.40))),
                (PersonalityArchetype.SocialButterfly, Math.Max(0.10, 0.90 + (tuning.FamilyFormationFactor * 1.10) + housedBias + marriedBias - (homelessBias * 0.30))));
        }

        private static T PickWeighted<T>(
            Random random,
            params (T item, double weight)[] items)
        {
            double total = 0;
            for (int i = 0; i < items.Length; i++)
                total += Math.Max(0.0, items[i].weight);

            if (total <= 0)
                return items[^1].item;

            double roll = random.NextDouble() * total;
            double accumulated = 0;

            for (int i = 0; i < items.Length; i++)
            {
                accumulated += Math.Max(0.0, items[i].weight);
                if (roll < accumulated)
                    return items[i].item;
            }

            return items[^1].item;
        }

        private static int ScaleFromNeutral(
            int percent,
            int maxMagnitude)
        {
            decimal normalized = (percent - 50) / 50m;
            return (int)Math.Round(
                d: normalized * maxMagnitude,
                mode: MidpointRounding.AwayFromZero);
        }

        private static int GetVolatilityJitter(
            Random random,
            BootstrapTuningModel tuning,
            int maxAbsMagnitude)
        {
            int adjustedMagnitude = Math.Max(
                1,
                (int)Math.Round(
                    d: maxAbsMagnitude * (0.35m + (decimal)tuning.SocialVolatilityFactor),
                    mode: MidpointRounding.AwayFromZero));

            return random.Next(
                minValue: -adjustedMagnitude,
                maxValue: adjustedMagnitude + 1);
        }

        private enum HouseholdComposition
        {
            MarriedFamily = 1,
            SingleParentFamily = 2,
            AdultOnly = 3
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

        private sealed class BootstrapTuningModel(
            int housingPressurePercent,
            int economicStabilityPercent,
            int socialVolatilityPercent,
            int familyFormationPercent)
        {
            public int HousingPressurePercent { get; } = Math.Clamp(housingPressurePercent, 0, 100);
            public int EconomicStabilityPercent { get; } = Math.Clamp(economicStabilityPercent, 0, 100);
            public int SocialVolatilityPercent { get; } = Math.Clamp(socialVolatilityPercent, 0, 100);
            public int FamilyFormationPercent { get; } = Math.Clamp(familyFormationPercent, 0, 100);

            public double HousingPressureFactor => HousingPressurePercent / 100d;
            public double EconomicStabilityFactor => EconomicStabilityPercent / 100d;
            public double SocialVolatilityFactor => SocialVolatilityPercent / 100d;
            public double FamilyFormationFactor => FamilyFormationPercent / 100d;

            public static BootstrapTuningModel From(CityPopulationBootstrapTuning tuning)
            {
                return new BootstrapTuningModel(
                    housingPressurePercent: tuning.HousingPressurePercent,
                    economicStabilityPercent: tuning.EconomicStabilityPercent,
                    socialVolatilityPercent: tuning.SocialVolatilityPercent,
                    familyFormationPercent: tuning.FamilyFormationPercent);
            }

            public static BootstrapTuningModel Default() => From(CityPopulationBootstrapTuning.Default());
        }
    }
}
