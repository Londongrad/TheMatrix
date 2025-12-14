using Matrix.Population.Domain.Entities;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Services
{
    public sealed class PopulationGenerator
    {
        #region [ Fields ]

        private static readonly string[] JobTitles =
        [
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
        ];

        #endregion [ Fields ]

        #region [ Generate ]

        public IReadOnlyCollection<Person> Generate(
            int peopleCount,
            DateOnly currentDate,
            int? randomSeed = null)
        {
            if (peopleCount <= 0)
                return [];

            Random random = randomSeed.HasValue
                ? new Random(randomSeed.Value)
                : new Random();

            var result = new List<Person>(peopleCount);

            for (int i = 0; i < peopleCount; i++)
            {
                Person person = CreateRandomPerson(
                    random: random,
                    householdId: HouseholdId.New(),
                    currentDate: currentDate);

                result.Add(person);
            }

            return result;
        }

        #endregion [ Generate ]

        #region [ CreateRandomPerson ]

        private Person CreateRandomPerson(
            Random random,
            HouseholdId householdId,
            DateOnly currentDate)
        {
            var personId = PersonId.New();

            Sex sex = CreateRandomSex(random);
            PersonName name = CreateRandomName(
                random: random,
                sex: sex);

            // Возраст в годах + BirthDate + AgeGroup
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

            HappinessLevel happiness =
                CreateInitialHappiness(
                    random: random,
                    ageGroup: ageGroup,
                    employmentStatus: employmentStatus);

            EducationLevel educationLevel = CreateRandomEducationLevel(
                r: random,
                ageYears: ageYears);

            MaritalStatus maritalStatus =
                CreateRandomMaritalStatus(
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

        #endregion [ CreateRandomPerson ]

        #region [ Marital ]

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
                {
                    // Молодые: в основном single, немного married
                    if (roll < 0.8)
                        return MaritalStatus.Single;
                    return MaritalStatus.Married;
                }

                if (ageYears < 40)
                {
                    // 25–39: больше married, немного divorced/single
                    if (roll < 0.6)
                        return MaritalStatus.Married;
                    if (roll < 0.8)
                        return MaritalStatus.Single;
                    return MaritalStatus.Divorced;
                }

                // 40–65: ещё больше браков и разводов
                if (roll < 0.6)
                    return MaritalStatus.Married;
                if (roll < 0.85)
                    return MaritalStatus.Divorced;
                return MaritalStatus.Single;
            }

            // Пенсионеры
            {
                double roll = random.NextDouble();
                if (roll < 0.5)
                    return MaritalStatus.Married;
                if (roll < 0.8)
                    return MaritalStatus.Widowed;
                return MaritalStatus.Divorced;
            }
        }

        #endregion [ Marital ]

        #region [ Sex, Name, Weight ]

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
            // Очень простой генератор имён, можно потом заменить на нормальный справочник.
            string[] maleFirstNames =
            [
                "Иван",
                "Алексей",
                "Михаил",
                "Дмитрий",
                "Сергей"
            ];
            string[] femaleFirstNames =
            [
                "Анна",
                "Мария",
                "Екатерина",
                "Ольга",
                "Наталья"
            ];
            string[] lastNames =
            [
                "Иванов",
                "Петров",
                "Сидоров",
                "Смирнов",
                "Ковалёв"
            ];

            string firstName = sex == Sex.Male
                ? maleFirstNames[random.Next(maleFirstNames.Length)]
                : femaleFirstNames[random.Next(femaleFirstNames.Length)];

            string lastName = lastNames[random.Next(lastNames.Length)];

            return new PersonName(
                firstName: firstName,
                lastName: lastName); // если есть отчество – добавишь третьим параметром
        }

        private static BodyWeight CreateRandomWeight(
            Random random,
            Sex sex,
            int ageYears)
        {
            decimal kg = ageYears switch
            {
                // младенцы/ранний возраст
                < 1 => random.Next(
                    minValue: 3,
                    maxValue: 11), // 3–10
                < 3 => random.Next(
                    minValue: 8,
                    maxValue: 16), // 8–15

                // дошкольники
                < 7 => random.Next(
                    minValue: 12,
                    maxValue: 26), // 12–25

                // дети
                < 13 => random.Next(
                    minValue: 20,
                    maxValue: 46), // 20–45

                // подростки (уже заметна разница по полу)
                < 18 => sex == Sex.Male
                    ? random.Next(
                        minValue: 40,
                        maxValue: 86) // 40–85
                    : random.Next(
                        minValue: 38,
                        maxValue: 76), // 38–75

                // взрослые
                < 66 => sex == Sex.Male
                    ? random.Next(
                        minValue: 60,
                        maxValue: 111) // 60–110
                    : random.Next(
                        minValue: 45,
                        maxValue: 96), // 45–95

                // пожилые
                _ => sex == Sex.Male
                    ? random.Next(
                        minValue: 55,
                        maxValue: 96) // 55–95
                    : random.Next(
                        minValue: 45,
                        maxValue: 86) // 45–85
            };

            return BodyWeight.FromKilograms(kg);
        }

        #endregion [ Sex, Name, Weight ]

        #region [ Health, Age, Happiness ]

        private static HealthLevel CreateRandomHealth(
            Random random,
            int ageYears)
        {
            int value = ageYears switch
            {
                < 7 => random.Next(
                    minValue: 70,
                    maxValue: 101), // маленькие дети
                < 18 => random.Next(
                    minValue: 60,
                    maxValue: 101), // подростки
                < 40 => random.Next(
                    minValue: 50,
                    maxValue: 96), // взрослые
                < 66 => random.Next(
                    minValue: 45,
                    maxValue: 91), // 40–65
                < 80 => random.Next(
                    minValue: 35,
                    maxValue: 86), // пожилые
                _ => random.Next(
                    minValue: 30,
                    maxValue: 81) // 80+
            };

            // Доп. спад после 40 (нарастающий)
            if (ageYears > 40)
            {
                int extraPenalty = Math.Min(
                    val1: 20,
                    val2: (ageYears - 40) / 2); // 0..20
                value -= random.Next(
                             minValue: 0,
                             maxValue: 6) +
                         extraPenalty; // -0..5 - extra
            }

            value = Math.Clamp(
                value: value,
                min: 0,
                max: 100);
            return HealthLevel.From(value);
        }

        private static int CreateRandomAgeYears(Random random)
        {
            // 20% дети, 60% взрослые, 20% пенсионеры.
            double roll = random.NextDouble();

            if (roll < 0.2) // дети
                return random.Next(
                    minValue: 0,
                    maxValue: 18); // [0..17]

            if (roll < 0.8) // взрослые
                return random.Next(
                    minValue: 18,
                    maxValue: 66); // [18..65]

            // пенсионеры
            return random.Next(
                minValue: 66,
                maxValue: 121); // [66..120]
        }

        private static HappinessLevel CreateInitialHappiness(
            Random random,
            AgeGroup ageGroup,
            EmploymentStatus employmentStatus)
        {
            // Базовый уровень 40–80
            int value = random.Next(
                minValue: 40,
                maxValue: 81); // [40..80]

            if (employmentStatus == EmploymentStatus.Employed && ageGroup == AgeGroup.Adult)
                value += random.Next(
                    minValue: 0,
                    maxValue: 11); // +0..10
            else
                if (employmentStatus == EmploymentStatus.Unemployed && ageGroup == AgeGroup.Adult)
                    value -= random.Next(
                        minValue: 5,
                        maxValue: 16); // -5..15
                else
                    if
                        (employmentStatus == EmploymentStatus.Retired)
                        value += random.Next(
                            minValue: 0,
                            maxValue: 6); // пенсионеры чуть довольнее

            value = Math.Clamp(
                value: value,
                min: 0,
                max: 100);
            return HappinessLevel.From(value);
        }

        #endregion [ Health, Age, Happiness ]

        #region [ Employment ]

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
            // Взрослые: 70% работают, 15% безработные, 15% студенты
            double roll = random.NextDouble();

            if (roll < 0.70)
                return EmploymentStatus.Employed;

            if (roll < 0.85)
                return EmploymentStatus.Unemployed;

            return EmploymentStatus.Student;
        }

        private static Job? CreateRandomJob(Random random)
        {
            string title = JobTitles[random.Next(JobTitles.Length)];
            var workplaceId = WorkplaceId.New();

            return new Job(
                workplaceId: workplaceId,
                title: title);
        }

        #endregion [ Employment ]

        #region [ Education ]

        private static EducationLevel CreateRandomEducationLevel(
            Random r,
            int ageYears)
        {
            return ageYears switch
            {
                // 0-2
                <= 2 => EducationLevel.None,

                // 3-6
                <= 6 => Pick(
                    r: r,
                    (EducationLevel.Preschool, 0.80),
                    (EducationLevel.None, 0.20)),

                // 7-10
                <= 10 => Pick(
                    r: r,
                    (EducationLevel.Primary, 0.97),
                    (EducationLevel.LowerSecondary, 0.03)), // редкое "ускорение"

                // 11-14
                <= 14 => Pick(
                    r: r,
                    (EducationLevel.LowerSecondary, 0.85),
                    (EducationLevel.Primary, 0.15)),

                // 15-17
                <= 17 => Pick(
                    r: r,
                    (EducationLevel.UpperSecondary, 0.72),
                    (EducationLevel.LowerSecondary, 0.18),
                    (EducationLevel.Vocational, 0.10)),

                // 18-21
                <= 21 => Pick(
                    r: r,
                    (EducationLevel.UpperSecondary, 0.25),
                    (EducationLevel.Vocational, 0.33),
                    (EducationLevel.Higher, 0.40),
                    (EducationLevel.LowerSecondary, 0.02)),

                // 22–65
                <= 65 => Pick(
                    r: r,
                    (EducationLevel.None, 0.01),
                    (EducationLevel.Primary, 0.08),
                    (EducationLevel.LowerSecondary, 0.22),
                    (EducationLevel.UpperSecondary, 0.30),
                    (EducationLevel.Vocational, 0.22),
                    (EducationLevel.Higher, 0.15),
                    (EducationLevel.Postgraduate, 0.02)),

                // 66+
                _ => Pick(
                    r: r,
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
            Random r,
            params (EducationLevel level, double weight)[] items)
        {
            double total = 0;
            for (int i = 0; i < items.Length; i++)
                total += items[i].weight;

            double roll = r.NextDouble() * total;
            double acc = 0;

            for (int i = 0; i < items.Length; i++)
            {
                acc += items[i].weight;
                if (roll < acc)
                    return items[i].level;
            }

            return items[^1].level;
        }

        #endregion [ Education ]
    }
}
