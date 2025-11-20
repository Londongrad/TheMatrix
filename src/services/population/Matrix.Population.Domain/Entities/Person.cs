using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class Person
    {
        #region [ Properties ]

        public PersonId Id { get; }
        public HouseholdId HouseholdId { get; }
        public DistrictId DistrictId { get; }

        public PersonName Name { get; private set; } = null!;

        public Sex Sex { get; }
        
        public LifeSpan LifeSpan { get; private set; } = null!;
        public LifeStatus LifeStatus { get; private set; }
        public bool IsAlive => LifeStatus == LifeStatus.Alive;
        public DateOnly BirthDate => LifeSpan.BirthDate;
        public DateOnly? DeathDate => LifeSpan.DeathDate;

        public MaritalStatus MaritalStatus { get; private set; }
        public EducationLevel EducationLevel { get; private set; }

        public HealthLevel Health { get; private set; }
        public BodyWeight Weight { get; private set; } = null!;

        public HappinessLevel Happiness { get; private set; }

        public EmploymentStatus EmploymentStatus { get; private set; }
        public Job? Job { get; private set; }

        public Personality Personality { get; } = null!;

        #endregion [ Properties ]

        #region [ Constructors ]

        // Для EF Core
        private Person() { }

        private Person(
            PersonId personId,
            HouseholdId householdId,
            DistrictId districtId,
            PersonName name,
            Sex sex,
            LifeSpan lifeSpan,
            LifeStatus lifeStatus,
            MaritalStatus maritalStatus,
            EducationLevel educationLevel,
            EmploymentStatus employmentStatus,
            HappinessLevel happinessLevel,
            Personality personality,
            HealthLevel healthLevel,
            BodyWeight weight,
            Job? job = null,
            DateOnly? currentDate = null)
        {
            Id = personId;
            HouseholdId = householdId;
            DistrictId = districtId;
            Happiness = happinessLevel;
            Health = healthLevel;
            Weight = weight;
            LifeSpan = lifeSpan;

            Job = job;

            Name = GuardHelper.AgainstNull(name, nameof(Name));
            Sex = GuardHelper.AgainstInvalidEnum(sex, nameof(Sex));
            LifeStatus = GuardHelper.AgainstInvalidEnum(lifeStatus, nameof(LifeStatus));
            MaritalStatus = GuardHelper.AgainstInvalidEnum(maritalStatus, nameof(MaritalStatus));
            EducationLevel = GuardHelper.AgainstInvalidEnum(educationLevel, nameof(EducationLevel));
            EmploymentStatus = GuardHelper.AgainstInvalidEnum(employmentStatus, nameof(EmploymentStatus));
            Personality = GuardHelper.AgainstNull(personality, nameof(Personality));

            if (currentDate is not null)
            {
                EnsureConsistency(currentDate.Value);
            }
        }

        #endregion [ Constructors ]

        #region [ Factory Methods ]

        /// <summary>
        /// Generic person creation.
        /// </summary>
        public static Person CreatePerson(
            PersonId id,
            HouseholdId householdId,
            DistrictId districtId,
            PersonName name,
            Sex sex,
            LifeStatus lifeStatus,
            MaritalStatus maritalStatus,
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

            return new Person(
                personId: id,
                householdId: householdId,
                districtId: districtId,
                name: name,
                sex: sex,
                lifeSpan: lifeSpan,
                lifeStatus: lifeStatus,
                maritalStatus: maritalStatus,
                educationLevel: educationLevel,
                employmentStatus: employmentStatus,
                happinessLevel: happinessLevel,
                personality: personality,
                healthLevel: healthLevel,
                weight: weight,
                job: job,
                currentDate: currentDate);
        }

        /// <summary>
        /// Creates a student person (alive, single, student, no job).
        /// </summary>
        public static Person CreateStudent(
            PersonId id,
            HouseholdId householdId,
            DistrictId districtId,
            PersonName name,
            Sex sex,
            DateOnly birthDate,
            EducationLevel educationLevel,
            HappinessLevel happinessLevel,
            HealthLevel healthLevel,
            BodyWeight weight,
            Personality personality,
            DateOnly currentDate)
        {
            var person = CreatePerson(
                id: id,
                householdId: householdId,
                districtId: districtId,
                name: name,
                sex: sex,
                birthDate: birthDate,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: MaritalStatus.Single,
                educationLevel: educationLevel,
                employmentStatus: EmploymentStatus.Student,
                happinessLevel: happinessLevel,
                personality: personality,
                weight: weight,
                healthLevel: healthLevel,
                job: null,
                currentDate: currentDate);

            var ageGroup = person.GetAgeGroup(currentDate);
            if (ageGroup == AgeGroup.Child)
            {
                throw new DomainValidationException(
                    "Student must not be a child.",
                    nameof(birthDate));
            }

            return person;
        }

        /// <summary>
        /// Creates an elderly / senior person (alive, retired, no job).
        /// </summary>
        public static Person CreateSenior(
            PersonId id,
            HouseholdId householdId,
            DistrictId districtId,
            PersonName name,
            Sex sex,
            DateOnly birthDate,
            MaritalStatus maritalStatus,
            EducationLevel educationLevel,
            HappinessLevel happinessLevel,
            Personality personality,
            HealthLevel healthLevel,
            BodyWeight weight,
            DateOnly currentDate)
        {
            var person = CreatePerson(
                id: id,
                householdId: householdId,
                districtId: districtId,
                name: name,
                sex: sex,
                birthDate: birthDate,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: maritalStatus,
                educationLevel: educationLevel,
                employmentStatus: EmploymentStatus.Retired,
                happinessLevel: happinessLevel,
                personality: personality,
                healthLevel: healthLevel,
                weight: weight,
                job: null,
                currentDate: currentDate);

            var ageGroup = person.GetAgeGroup(currentDate);
            if (ageGroup != AgeGroup.Senior)
            {
                throw new DomainValidationException(
                    "Senior person must be in retired age group.",
                    nameof(birthDate));
            }

            return person;
        }

        /// <summary>
        /// Newborn person creation.
        /// </summary>
        public static Person CreateNewborn(
            PersonId id,
            HouseholdId householdId,
            DistrictId districtId,
            PersonName name,
            Sex sex,
            DateOnly birthDate,
            BodyWeight weight,
            Personality personality,
            DateOnly currentDate)
        {
            return CreatePerson(
                id: id,
                householdId: householdId,
                districtId: districtId,
                name: name,
                sex: sex,
                birthDate: birthDate,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: MaritalStatus.Single,
                educationLevel: EducationLevel.None,
                employmentStatus: EmploymentStatus.None,
                happinessLevel: HappinessLevel.Default(),
                healthLevel: HealthLevel.Default(),
                weight: weight,
                personality: personality,
                job: null,
                currentDate: currentDate);
        }

        /// <summary>
        /// Adult person creation.
        /// </summary>
        public static Person CreateAdult(
            PersonId id,
            HouseholdId householdId,
            DistrictId districtId,
            PersonName name,
            Sex sex,
            DateOnly birthDate,
            EmploymentStatus employmentStatus,
            Personality personality,
            HealthLevel healthLevel,
            BodyWeight weight,
            Job? job,
            DateOnly currentDate)
        {
            return CreatePerson(
                id: id,
                householdId: householdId,
                districtId: districtId,
                name: name,
                sex: sex,
                birthDate: birthDate,
                lifeStatus: LifeStatus.Alive,
                maritalStatus: MaritalStatus.Single,
                educationLevel: EducationLevel.Primary,
                employmentStatus: employmentStatus,
                happinessLevel: HappinessLevel.Default(),
                weight: weight,
                healthLevel: healthLevel,
                personality: personality,
                job: job,
                currentDate: currentDate);
        }

        #endregion [ Factory Methods ]

        #region [ Methods ]

        public Age GetAge(DateOnly currentDate) =>
            LifeSpan.GetAge(currentDate);

        public AgeGroup GetAgeGroup(DateOnly currentDate) =>
            AgeGroupRules.GetAgeGroup(GetAge(currentDate));

        #region [ Happiness ]

        public void SetHappiness(int value)
        {
            Happiness = HappinessLevel.From(value);
        }

        public void ChangeHappiness(int delta)
        {
            // Character traits may modify the happiness change.
            var finalDelta = Personality.ModifyHappinessDelta(delta);
            Happiness = Happiness.WithDelta(finalDelta);
        }

        #endregion [ Happiness ]

        #region [ Health ]

        public void ChangeHealth(int delta, DateOnly currentDate)
        {
            if (!IsAlive)
                return;

            Health = Health.WithDelta(delta);

            // If health drops to zero, the person dies.
            if (Health.IsDead)
            {
                Die(currentDate);
                return;
            }

            EnsureConsistency(currentDate);
        }

        #endregion [ Health ]

        #region [ Name ]

        public void ChangeName(PersonName newName)
        {
            Name = GuardHelper.AgainstNull(newName, nameof(Name));
        }

        #endregion [ Name ]

        #region [ Education ]

        public void SetEducationLevel(EducationLevel newLevel)
        {
            EducationLevel = GuardHelper.AgainstInvalidEnum(newLevel, nameof(EducationLevel));
        }

        #endregion [ Education ]

        #region [ Employment ]

        public void AssignJob(DateOnly currentDate, Job job)
        {
            if (!IsAlive)
                throw new DomainValidationException("Deceased person cannot be employed.", nameof(LifeStatus));

            if (GetAgeGroup(currentDate) != AgeGroup.Adult)
                throw new DomainValidationException("Only adults can be employed.", nameof(EmploymentStatus));

            Job = GuardHelper.AgainstNull(job, nameof(Job));
            EmploymentStatus = EmploymentStatus.Employed;
            ChangeHappiness(+10);
            EnsureConsistency(currentDate);
        }

        public void Fire(DateOnly currentDate)
        {
            if (!IsAlive)
                throw new DomainValidationException("Deceased person cannot be fired.", nameof(LifeStatus));

            if (EmploymentStatus != EmploymentStatus.Employed)
                throw new DomainValidationException("Unemplyeed person cannot be fired.", nameof(LifeStatus));

            Job = null;
            EmploymentStatus = EmploymentStatus.Unemployed;
            ChangeHappiness(-10);
            EnsureConsistency(currentDate);
        }

        public void Retire(DateOnly currentDate)
        {
            if (!IsAlive)
                throw new DomainValidationException("Deceased person cannot retire.", nameof(LifeStatus));

            var ageGroup = GetAgeGroup(currentDate);

            if (ageGroup != AgeGroup.Senior)
                throw new DomainValidationException("Only seniors can retire.", nameof(EmploymentStatus));

            Job = null;
            EmploymentStatus = EmploymentStatus.Retired;
            ChangeHappiness(+5);
            EnsureConsistency(currentDate);
        }

        public void SetEmploymentStatus(DateOnly currentDate, EmploymentStatus newStatus, Job? job = null)
        {
            GuardHelper.AgainstInvalidEnum(newStatus, nameof(newStatus));
            switch (newStatus)
            {
                case EmploymentStatus.Employed:
                    if (job is null)
                        throw new DomainValidationException("Job must be provided when changing status to Employed.", nameof(job));
                    AssignJob(currentDate, job);
                    break;
                case EmploymentStatus.Unemployed:
                    Fire(currentDate);
                    break;
                case EmploymentStatus.Retired:
                    Retire(currentDate);
                    break;
                case EmploymentStatus.Student:
                    if (GetAgeGroup(currentDate) is AgeGroup.Child or AgeGroup.Senior)
                        throw new DomainValidationException("Child/Senior cannot be a student.", nameof(EmploymentStatus));
                    Job = null;
                    EmploymentStatus = EmploymentStatus.Student;
                    ChangeHappiness(+5);
                    EnsureConsistency(currentDate);
                    break;
                case EmploymentStatus.None:
                    Job = null;
                    EmploymentStatus = EmploymentStatus.None;
                    ChangeHappiness(-5);
                    EnsureConsistency(currentDate);
                    break;
            }
        }

        #endregion [ Employment ]

        #region [ Marital ]

        public void Marry()
        {
            if (!IsAlive)
                throw new DomainValidationException("Deceased person cannot marry.", nameof(LifeStatus));
            if (MaritalStatus == MaritalStatus.Married)
                throw new DomainValidationException("Person is already married.", nameof(MaritalStatus));
            MaritalStatus = MaritalStatus.Married;
            ChangeHappiness(+15);
        }

        public void Divorce()
        {
            if (!IsAlive)
                throw new DomainValidationException("Deceased person cannot divorce.", nameof(LifeStatus));
            if (MaritalStatus == MaritalStatus.Single)
                throw new DomainValidationException("Person is not married.", nameof(MaritalStatus));
            MaritalStatus = MaritalStatus.Single;
            ChangeHappiness(-15);
        }

        public void Widow()
        {
            if (!IsAlive)
                throw new DomainValidationException("Deceased person cannot become a widow(er).", nameof(LifeStatus));
            if (MaritalStatus == MaritalStatus.Single)
                throw new DomainValidationException("Person is not married.", nameof(MaritalStatus));
            MaritalStatus = MaritalStatus.Widowed;
            ChangeHappiness(-20);
        }

        public void ChangeMaritalStatus(MaritalStatus newStatus)
        {
            GuardHelper.AgainstInvalidEnum(newStatus, nameof(newStatus));
            switch (newStatus)
            {
                case MaritalStatus.Single:
                    Divorce();
                    break;
                case MaritalStatus.Married:
                    Marry();
                    break;
                case MaritalStatus.Widowed:
                    Widow();
                    break;
            }
        }

        #endregion [ Marital ]

        #region [ Die/Ressurect ]

        public void Die(DateOnly currentDate)
        {
            if (!IsAlive)
                throw new DomainValidationException("Person is already dead.");

            LifeStatus = LifeStatus.Deceased;
            LifeSpan = LifeSpan.WithDeath(currentDate);

            Job = null;
            EmploymentStatus = EmploymentStatus.None;
            Health = HealthLevel.From(0);

            EnsureConsistencyForDead();
        }

        public void Resurrect(DateOnly currentDate)
        {
            if (IsAlive)
                throw new DomainValidationException("Person is already alive.");

            LifeStatus = LifeStatus.Alive;
            LifeSpan = LifeSpan.Resurrect();
            Health = HealthLevel.From(100);

            EnsureConsistency(currentDate);
        }

        #endregion [ Die/Ressurect ]

        #region [ Consistency Checks ]

        private void EnsureConsistencyForDead()
        {
            if (IsAlive)
            {
                throw new DomainValidationException(
                    $"{nameof(EnsureConsistencyForDead)} called for an alive person.",
                    nameof(LifeStatus));
            }

            if (DeathDate is null)
            {
                throw new DomainValidationException(
                    "Deceased person must have a death date.",
                    nameof(LifeSpan));
            }

            if (EmploymentStatus != EmploymentStatus.None)
            {
                throw new DomainValidationException(
                    "Deceased person should have employmentStatus = None.",
                    nameof(EmploymentStatus));
            }

            if (Job is not null)
            {
                throw new DomainValidationException(
                    "Deceased person cannot have a job.",
                    nameof(Job));
            }

            if (!Health.IsDead)
            {
                throw new DomainValidationException(
                    "Deceased person must have zero health.",
                    nameof(Health));
            }
        }

        private void EnsureConsistency(DateOnly currentDate)
        {
            // Согласованность LifeStatus <-> LifeSpan
            if (LifeStatus == LifeStatus.Alive && DeathDate is not null)
            {
                throw new DomainValidationException(
                    "Alive person cannot have a death date.",
                    nameof(LifeStatus));
            }

            if (LifeStatus == LifeStatus.Deceased && DeathDate is null)
            {
                throw new DomainValidationException(
                    "Deceased person must have a death date.",
                    nameof(LifeStatus));
            }

            if (!IsAlive)
            {
                EnsureConsistencyForDead();
                return;
            }

            var group = GetAgeGroup(currentDate);

            if (group == AgeGroup.Child &&
                EmploymentStatus == EmploymentStatus.Employed)
            {
                throw new DomainValidationException("Child cannot be employed.", nameof(EmploymentStatus));
            }

            if (group == AgeGroup.Senior &&
                (EmploymentStatus == EmploymentStatus.Employed ||
                 EmploymentStatus == EmploymentStatus.Student))
            {
                throw new DomainValidationException(
                    "Retired person cannot be employed or a student.",
                    nameof(EmploymentStatus));
            }

            if (EmploymentStatus == EmploymentStatus.Employed && Job is null)
            {
                throw new DomainValidationException("Employed person must have a job.", nameof(Job));
            }

            if (EmploymentStatus != EmploymentStatus.Employed && Job is not null)
            {
                throw new DomainValidationException("Only employed person can have a job.", nameof(Job));
            }
        }

        #endregion [ Consistency Checks ]

        #endregion [ Methods ]
    }
}
