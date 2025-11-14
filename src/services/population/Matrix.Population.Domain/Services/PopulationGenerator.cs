using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Services
{
    public sealed class PopulationGenerator(
        int districtCount = 5,
        int averageHouseholdSize = 3)
    {
        private readonly int _districtCount = Math.Max(1, districtCount);
        private readonly int _averageHouseholdSize = Math.Max(1, averageHouseholdSize);

        public IReadOnlyList<Person> Generate(
            int peopleCount,
            DateOnly currentDate,
            int? randomSeed = null)
        {
            if (peopleCount <= 0)
                return Array.Empty<Person>();

            var random = randomSeed.HasValue
                ? new Random(randomSeed.Value)
                : new Random();

            var result = new List<Person>(peopleCount);

            var districts = CreateDistricts(_districtCount);
            var households = CreateHouseholds(random, peopleCount, districts);

            for (int i = 0; i < peopleCount; i++)
            {
                var (householdId, districtId) =
                    households[random.Next(households.Count)];

                var person = CreateRandomPerson(
                    random,
                    householdId,
                    districtId,
                    currentDate);

                result.Add(person);
            }

            return result;
        }

        // ----------------- Создание одного Person -----------------

        private Person CreateRandomPerson(
            Random random,
            HouseholdId householdId,
            DistrictId districtId,
            DateOnly currentDate)
        {
            var personId = PersonId.New();

            var sex = CreateRandomSex(random);
            var name = CreateRandomName(random, sex);

            // Возраст в годах + BirthDate + AgeGroup
            var ageYears = CreateRandomAgeYears(random);
            var birthDate = currentDate.AddYears(-ageYears);
            var age = Age.FromYears(ageYears);
            var ageGroup = AgeGroupRules.GetAgeGroup(age);

            var personality = Personality.CreateRandom(random);

            var employmentStatus = CreateRandomEmploymentStatus(random, ageGroup);
            var job = employmentStatus == EmploymentStatus.Employed
                ? CreateRandomJob(random)
                : null;

            var happiness = CreateInitialHappiness(random, ageGroup, employmentStatus);

            var educationLevel = CreateRandomEducationLevel(random, ageGroup, ageYears);
            var maritalStatus = CreateRandomMaritalStatus(random, ageGroup, ageYears);

            // Выбор фабрики Person в зависимости от возраста/статуса
            return ageGroup switch
            {
                AgeGroup.Child => Person.CreateNewborn(
                    id: personId,
                    householdId: householdId,
                    districtId: districtId,
                    name: name,
                    sex: sex,
                    birthDate: birthDate,
                    personality: personality,
                    currentDate: currentDate),

                AgeGroup.Youth or AgeGroup.Adult when employmentStatus == EmploymentStatus.Student =>
                    Person.CreateStudent(
                        id: personId,
                        householdId: householdId,
                        districtId: districtId,
                        name: name,
                        sex: sex,
                        birthDate: birthDate,
                        educationLevel: educationLevel,
                        happinessLevel: happiness,
                        personality: personality,
                        currentDate: currentDate),

                AgeGroup.Youth or AgeGroup.Adult =>
                    Person.CreateAdult(
                        id: personId,
                        householdId: householdId,
                        districtId: districtId,
                        name: name,
                        sex: sex,
                        birthDate: birthDate,
                        employmentStatus: employmentStatus,
                        personality: personality,
                        job: job,
                        currentDate: currentDate),

                AgeGroup.Senior =>
                    Person.CreateSenior(
                        id: personId,
                        householdId: householdId,
                        districtId: districtId,
                        name: name,
                        sex: sex,
                        birthDate: birthDate,
                        maritalStatus: maritalStatus,
                        educationLevel: educationLevel,
                        happinessLevel: happiness,
                        personality: personality,
                        currentDate: currentDate),

                _ => throw new InvalidOperationException("Unknown age group")
            };
        }

        // ----------------- Черты человека -----------------

        private Sex CreateRandomSex(Random random) =>
            random.Next(0, 2) == 0 ? Sex.Male : Sex.Female;

        private PersonName CreateRandomName(Random random, Sex sex)
        {
            // Очень простой генератор имён, можно потом заменить на нормальный справочник.
            string[] maleFirstNames = ["Иван", "Алексей", "Михаил", "Дмитрий", "Сергей"];
            string[] femaleFirstNames = ["Анна", "Мария", "Екатерина", "Ольга", "Наталья"];
            string[] lastNames = ["Иванов", "Петров", "Сидоров", "Смирнов", "Ковалёв"];

            var firstName = sex == Sex.Male
                ? maleFirstNames[random.Next(maleFirstNames.Length)]
                : femaleFirstNames[random.Next(femaleFirstNames.Length)];

            var lastName = lastNames[random.Next(lastNames.Length)];

            return new PersonName(firstName, lastName); // если есть отчество – добавишь третьим параметром
        }

        private int CreateRandomAgeYears(Random random)
        {
            // 20% дети, 60% взрослые, 20% пенсионеры.
            var roll = random.NextDouble();

            if (roll < 0.2)        // дети
                return random.Next(0, 18);    // [0..17]

            if (roll < 0.8)        // взрослые
                return random.Next(18, 66);   // [18..65]

            // пенсионеры
            return random.Next(66, 121);      // [66..120]
        }

        private EmploymentStatus CreateRandomEmploymentStatus(
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

        private EmploymentStatus CreateRandomAdultEmploymentStatus(Random random)
        {
            // Взрослые: 70% работают, 15% безработные, 15% студенты
            var roll = random.NextDouble();

            if (roll < 0.70)
                return EmploymentStatus.Employed;

            if (roll < 0.85)
                return EmploymentStatus.Unemployed;

            return EmploymentStatus.Student;
        }

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

        private Job? CreateRandomJob(Random random)
        {
            var title = JobTitles[random.Next(JobTitles.Length)];
            var workplaceId = new WorkplaceId(Guid.NewGuid());

            return new Job(workplaceId, title);
        }

        private HappinessLevel CreateInitialHappiness(
            Random random,
            AgeGroup ageGroup,
            EmploymentStatus employmentStatus)
        {
            // Базовый уровень 40–80
            var value = random.Next(40, 81); // [40..80]

            if (employmentStatus == EmploymentStatus.Employed && ageGroup == AgeGroup.Adult)
            {
                value += random.Next(0, 11); // +0..10
            }
            else if (employmentStatus == EmploymentStatus.Unemployed && ageGroup == AgeGroup.Adult)
            {
                value -= random.Next(5, 16); // -5..15
            }
            else if (employmentStatus == EmploymentStatus.Retired)
            {
                value += random.Next(0, 6); // пенсионеры чуть довольнее
            }

            value = Math.Clamp(value, 0, 100);
            return HappinessLevel.From(value);
        }

        private EducationLevel CreateRandomEducationLevel(
            Random random,
            AgeGroup ageGroup,
            int ageYears)
        {
            return ageGroup switch
            {
                AgeGroup.Child => ageYears switch
                {
                    < 6 => EducationLevel.None,
                    < 15 => EducationLevel.Primary,
                    _ => EducationLevel.Secondary
                },

                AgeGroup.Adult => GetAdultEducation(random),

                AgeGroup.Senior => GetSeniorEducation(random),

                _ => EducationLevel.None
            };

            EducationLevel GetAdultEducation(Random r)
            {
                var roll = r.NextDouble();
                if (roll < 0.2) return EducationLevel.Primary;
                if (roll < 0.8) return EducationLevel.Secondary;
                return EducationLevel.Higher;
            }

            EducationLevel GetSeniorEducation(Random r)
            {
                var roll = r.NextDouble();
                if (roll < 0.6) return EducationLevel.Primary;
                if (roll < 0.95) return EducationLevel.Secondary;
                return EducationLevel.Higher;
            }
        }

        private MaritalStatus CreateRandomMaritalStatus(
            Random random,
            AgeGroup ageGroup,
            int ageYears)
        {
            if (ageGroup == AgeGroup.Child)
                return MaritalStatus.Single;

            if (ageGroup == AgeGroup.Adult)
            {
                var roll = random.NextDouble();

                if (ageYears < 25)
                {
                    // Молодые: в основном single, немного married
                    if (roll < 0.8) return MaritalStatus.Single;
                    return MaritalStatus.Married;
                }

                if (ageYears < 40)
                {
                    // 25–39: больше married, немного divorced/single
                    if (roll < 0.6) return MaritalStatus.Married;
                    if (roll < 0.8) return MaritalStatus.Single;
                    return MaritalStatus.Divorced;
                }

                // 40–65: ещё больше браков и разводов
                if (roll < 0.6) return MaritalStatus.Married;
                if (roll < 0.85) return MaritalStatus.Divorced;
                return MaritalStatus.Single;
            }

            // Пенсионеры
            {
                var roll = random.NextDouble();
                if (roll < 0.5) return MaritalStatus.Married;
                if (roll < 0.8) return MaritalStatus.Widowed;
                return MaritalStatus.Divorced;
            }
        }

        // ----------------- Districts / Households -----------------

        private IReadOnlyList<DistrictId> CreateDistricts(int districtCount)
        {
            var districts = new List<DistrictId>(districtCount);

            for (int i = 0; i < districtCount; i++)
            {
                districts.Add(DistrictId.New());
            }

            return districts;
        }

        private IReadOnlyList<(HouseholdId HouseholdId, DistrictId DistrictId)> CreateHouseholds(
            Random random,
            int peopleCount,
            IReadOnlyList<DistrictId> districts)
        {
            var householdCount = Math.Max(1, peopleCount / _averageHouseholdSize);

            var households = new List<(HouseholdId, DistrictId)>(householdCount);

            for (int i = 0; i < householdCount; i++)
            {
                var district = districts[random.Next(districts.Count)];
                var householdId = HouseholdId.New();

                households.Add((householdId, district));
            }

            return households;
        }
    }
}
