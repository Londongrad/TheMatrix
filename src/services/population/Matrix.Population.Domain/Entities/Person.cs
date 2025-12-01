using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class Person
    {
        #region [ Properties ]

        public PersonId Id { get; private set; }
        public HouseholdId HouseholdId { get; private set; }

        public PersonName Name { get; private set; } = null!;
        public Sex Sex { get; private set; }

        public LifeState Life { get; private set; } = null!;

        public MaritalInfo Marital { get; private set; } = null!;
        public EducationInfo Education { get; private set; } = null!;
        public EmploymentInfo Employment { get; private set; } = null!;

        public BodyWeight Weight { get; private set; } = null!;
        public HappinessLevel Happiness { get; private set; }
        public Personality Personality { get; private set; } = null!;

        #endregion [ Properties ]

        #region [ Convenience shortcuts ]

        public bool IsAlive => Life.Status == LifeStatus.Alive;
        public LifeStatus LifeStatus => Life.Status;
        public DateOnly BirthDate => Life.Span.BirthDate;
        public DateOnly? DeathDate => Life.Span.DeathDate;
        public HealthLevel Health => Life.Health;

        public MaritalStatus MaritalStatus => Marital.Status;
        public PersonId? SpouseId => Marital.SpouseId;

        public EducationLevel EducationLevel => Education.Level;

        #endregion [ Convenience shortcuts ]

        #region [ Constructors ]

        // Для EF Core
        private Person() { }

        private Person(
            PersonId id,
            HouseholdId householdId,
            PersonName name,
            Sex sex,
            LifeState life,
            MaritalInfo marital,
            EducationInfo education,
            EmploymentInfo employment,
            HappinessLevel happiness,
            Personality personality,
            BodyWeight weight)
        {
            Id = id;
            HouseholdId = householdId;

            Name = GuardHelper.AgainstNull(name, nameof(Name));
            Sex = GuardHelper.AgainstInvalidEnum(sex, nameof(Sex));

            Life = GuardHelper.AgainstNull(life, nameof(Life));
            Marital = GuardHelper.AgainstNull(marital, nameof(Marital));
            Education = GuardHelper.AgainstNull(education, nameof(Education));
            Employment = GuardHelper.AgainstNull(employment, nameof(Employment));

            Happiness = happiness;
            Personality = GuardHelper.AgainstNull(personality, nameof(Personality));
            Weight = GuardHelper.AgainstNull(weight, nameof(Weight));
        }

        #endregion [ Constructors ]

        #region [ Factory Methods ]

        /// <summary>
        /// Generic person creation.
        /// </summary>
        public static Person CreatePerson(
            PersonId id,
            HouseholdId householdId,
            PersonName name,
            Sex sex,
            LifeStatus lifeStatus,
            MaritalStatus maritalStatus,
            PersonId? spouseId,
            EducationLevel educationLevel,
            EmploymentStatus employmentStatus,
            HappinessLevel happinessLevel,
            Personality personality,
            DateOnly birthDate,
            HealthLevel healthLevel,
            BodyWeight weight,
            Job? job,
            DateOnly currentDate)
        {
            var lifeSpan = LifeSpan.FromBirthDate(birthDate);
            var life = LifeState.Create(
                status: lifeStatus,
                span: lifeSpan,
                health: healthLevel);

            var age = lifeSpan.GetAge(currentDate);
            var ageGroup = AgeGroupRules.GetAgeGroup(age);

            var employment = EmploymentInfo.Create(
                employmentStatus,
                job,
                lifeStatus,
                ageGroup);

            var marital = MaritalInfo.FromStatus(maritalStatus, spouseId);
            var education = EducationInfo.FromLevel(educationLevel);

            return new Person(
                id: id,
                householdId: householdId,
                name: name,
                sex: sex,
                life: life,
                marital: marital,
                education: education,
                employment: employment,
                happiness: happinessLevel,
                personality: personality,
                weight: weight);
        }

        #endregion [ Factory Methods ]

        #region [ Methods ]

        #region [ Age ]

        public Age GetAge(DateOnly currentDate) =>
            Life.Span.GetAge(currentDate);

        public AgeGroup GetAgeGroup(DateOnly currentDate) =>
            AgeGroupRules.GetAgeGroup(GetAge(currentDate));

        #endregion [ Age ]

        #region [ Happiness ]

        public void ChangeHappiness(int delta)
        {
            var finalDelta = Personality.ModifyHappinessDelta(delta);
            Happiness = Happiness.WithDelta(finalDelta);
        }

        #endregion [ Happiness ]

        #region [ Health / Life ]

        public void ChangeHealth(int delta, DateOnly currentDate)
        {
            var wasAlive = IsAlive;

            Life = Life.WithHealthDelta(delta, currentDate);

            if (wasAlive && !IsAlive)
            {
                // Человек умер из-за здоровья - чистим работу
                Employment = Employment.Change(
                    newStatus: EmploymentStatus.None,
                    newJob: null,
                    lifeStatus: LifeStatus,
                    ageGroup: GetAgeGroup(currentDate));

                // Возможно: создать событие PersonDiedEvent
                // AddDomainEvent(new PersonDiedEvent(Id, currentDate, Marital.SpouseId));
            }
        }

        public void Die(DateOnly currentDate)
        {
            Life = Life.Change(
                newStatus: LifeStatus.Deceased,
                newHealth: HealthLevel.From(0),
                newDeathDate: currentDate);

            Employment = Employment.Change(
                EmploymentStatus.None,
                newJob: null,
                lifeStatus: LifeStatus,
                ageGroup: GetAgeGroup(currentDate));
        }

        public void Resurrect()
        {
            Life = Life.Change(
                newStatus: LifeStatus.Alive,
                newHealth: HealthLevel.From(100),
                newDeathDate: null);

            // AddDomainEvent(new PersonResurrectedEvent(Id, currentDate));
        }

        #endregion [ Health / Life ]

        #region [ Name ]

        public void ChangeName(PersonName newName)
        {
            Name = GuardHelper.AgainstNull(newName, nameof(Name));
        }

        #endregion [ Name ]

        #region [ Education ]

        /// <summary>
        /// Жёсткая установка уровня (для генерации / миграций).
        /// </summary>
        public void SetEducationLevel(EducationLevel newLevel)
        {
            Education = EducationInfo.FromLevel(newLevel);
        }

        /// <summary>
        /// Доменный переход: человек получает новый уровень образования.
        /// </summary>
        public void GraduateTo(EducationLevel newLevel)
        {
            Education = Education.GraduateTo(newLevel);
            ChangeHappiness(PersonHappinessDeltas.OnGraduate);
        }

        #endregion [ Education ]

        #region [ Employment ]

        public void AssignJob(DateOnly currentDate, Job job)
        {
            Employment = Employment.Change(
                newStatus: EmploymentStatus.Employed,
                newJob: GuardHelper.AgainstNull(job, nameof(job)),
                lifeStatus: LifeStatus,
                ageGroup: GetAgeGroup(currentDate));

            ChangeHappiness(PersonHappinessDeltas.OnJobAssigned);
        }

        public void Fire(DateOnly currentDate)
        {
            Employment = Employment.Change(
                newStatus: EmploymentStatus.Unemployed,
                newJob: null,
                lifeStatus: LifeStatus,
                ageGroup: GetAgeGroup(currentDate));

            ChangeHappiness(PersonHappinessDeltas.OnFired);
        }

        public void Retire(DateOnly currentDate)
        {
            Employment = Employment.Change(
                newStatus: EmploymentStatus.Retired,
                newJob: null,
                lifeStatus: LifeStatus,
                ageGroup: GetAgeGroup(currentDate));

            ChangeHappiness(PersonHappinessDeltas.OnRetired);
        }

        #endregion [ Employment ]

        #region [ Marital ]

        public void Marry(PersonId spouseId)
        {
            Marital = MaritalInfo.MarriedWith(spouseId);
            ChangeHappiness(PersonHappinessDeltas.OnMarry);
        }

        public void Divorce()
        {
            Marital = MaritalInfo.Single();
            ChangeHappiness(PersonHappinessDeltas.OnDivorce);
        }

        public void BecomeWidowed()
        {
            Marital = MaritalInfo.Widowed();
            ChangeHappiness(PersonHappinessDeltas.OnWidow);
        }

        #endregion [ Marital ]

        #endregion [ Methods ]
    }
}
