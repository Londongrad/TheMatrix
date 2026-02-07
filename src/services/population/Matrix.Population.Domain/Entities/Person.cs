using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Models;
using Matrix.Population.Domain.Rules;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Entities
{
    public sealed class Person
    {
        #region [ Factory Methods ]

        /// <summary>
        ///     Generic person creation.
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
            EnergyLevel energyLevel,
            StressLevel stressLevel,
            SocialNeedLevel socialNeedLevel,
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

            Age age = lifeSpan.GetAge(currentDate);
            AgeGroup ageGroup = AgeGroupRules.GetAgeGroup(age);

            var employment = EmploymentInfo.Create(
                status: employmentStatus,
                job: job,
                lifeStatus: lifeStatus,
                ageGroup: ageGroup);

            var marital = MaritalInfo.FromStatus(
                status: maritalStatus,
                spouseId: spouseId);
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
                energy: energyLevel,
                stress: stressLevel,
                socialNeed: socialNeedLevel,
                personality: personality,
                weight: weight);
        }

        #endregion [ Factory Methods ]

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
        public EnergyLevel Energy { get; private set; }
        public StressLevel Stress { get; private set; }
        public SocialNeedLevel SocialNeed { get; private set; }
        public Personality Personality { get; } = null!;

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
            EnergyLevel energy,
            StressLevel stress,
            SocialNeedLevel socialNeed,
            Personality personality,
            BodyWeight weight)
        {
            Id = id;
            HouseholdId = householdId;

            Name = GuardHelper.AgainstNull(
                value: name,
                propertyName: nameof(Name));
            Sex = GuardHelper.AgainstInvalidEnum(
                value: sex,
                propertyName: nameof(Sex));

            Life = GuardHelper.AgainstNull(
                value: life,
                propertyName: nameof(Life));
            Marital = GuardHelper.AgainstNull(
                value: marital,
                propertyName: nameof(Marital));
            Education = GuardHelper.AgainstNull(
                value: education,
                propertyName: nameof(Education));
            Employment = GuardHelper.AgainstNull(
                value: employment,
                propertyName: nameof(Employment));

            Happiness = happiness;
            Energy = energy;
            Stress = stress;
            SocialNeed = socialNeed;
            Personality = GuardHelper.AgainstNull(
                value: personality,
                propertyName: nameof(Personality));
            Weight = GuardHelper.AgainstNull(
                value: weight,
                propertyName: nameof(Weight));
        }

        #endregion [ Constructors ]

        #region [ Methods ]

        #region [ Age ]

        public Age GetAge(DateOnly currentDate)
        {
            return Life.Span.GetAge(currentDate);
        }

        public AgeGroup GetAgeGroup(DateOnly currentDate)
        {
            return AgeGroupRules.GetAgeGroup(GetAge(currentDate));
        }

        #endregion [ Age ]

        #region [ Needs / Happiness ]

        public void ChangeHappiness(int delta)
        {
            int finalDelta = Personality.ModifyHappinessDelta(delta);
            Happiness = Happiness.WithDelta(finalDelta);
        }

        public void ChangeEnergy(int delta)
        {
            Energy = Energy.WithDelta(delta);
        }

        public void ChangeStress(int delta)
        {
            Stress = Stress.WithDelta(delta);
        }

        public void ChangeSocialNeed(int delta)
        {
            SocialNeed = SocialNeed.WithDelta(delta);
        }

        public bool ApplyNeedsProgression(
            PersonNeedsProgressionEffect effect,
            DateOnly currentDate)
        {
            effect = GuardHelper.AgainstNull(
                value: effect,
                propertyName: nameof(effect));

            if (!effect.HasAnyEffect || !IsAlive)
                return false;

            int previousEnergy = Energy.Value;
            int previousStress = Stress.Value;
            int previousSocialNeed = SocialNeed.Value;
            int previousHealth = Health.Value;
            int previousHappiness = Happiness.Value;
            bool wasAlive = IsAlive;

            if (effect.EnergyDelta != 0)
                ChangeEnergy(effect.EnergyDelta);

            if (effect.StressDelta != 0)
                ChangeStress(effect.StressDelta);

            if (effect.SocialNeedDelta != 0)
                ChangeSocialNeed(effect.SocialNeedDelta);

            if (effect.HealthDelta != 0)
                ChangeHealth(
                    delta: effect.HealthDelta,
                    currentDate: currentDate);

            if (effect.HappinessDelta != 0 && IsAlive)
                ChangeHappiness(effect.HappinessDelta);

            return previousEnergy != Energy.Value ||
                   previousStress != Stress.Value ||
                   previousSocialNeed != SocialNeed.Value ||
                   previousHealth != Health.Value ||
                   previousHappiness != Happiness.Value ||
                   wasAlive != IsAlive;
        }

        #endregion [ Needs / Happiness ]

        #region [ Health / Life ]

        public void ChangeHealth(
            int delta,
            DateOnly currentDate)
        {
            bool wasAlive = IsAlive;

            Life = Life.WithHealthDelta(
                delta: delta,
                currentDate: currentDate);

            if (wasAlive && !IsAlive)
            {
                ClearNeedsForDeath();
                Employment = Employment.Change(
                    newStatus: EmploymentStatus.None,
                    newJob: null,
                    lifeStatus: LifeStatus,
                    ageGroup: GetAgeGroup(currentDate));
            }
        }

        public void Die(DateOnly currentDate)
        {
            Life = Life.Change(
                newStatus: LifeStatus.Deceased,
                newHealth: HealthLevel.From(0),
                newDeathDate: currentDate);

            Employment = Employment.Change(
                newStatus: EmploymentStatus.None,
                newJob: null,
                lifeStatus: LifeStatus,
                ageGroup: GetAgeGroup(currentDate));

            ClearNeedsForDeath();
        }

        public void Resurrect()
        {
            Life = Life.Change(
                newStatus: LifeStatus.Alive,
                newHealth: HealthLevel.From(100),
                newDeathDate: null);

            Energy = EnergyLevel.Default();
            Stress = StressLevel.Default();
            SocialNeed = SocialNeedLevel.Default();
        }

        #endregion [ Health / Life ]

        #region [ Name ]

        public void ChangeName(PersonName newName)
        {
            Name = GuardHelper.AgainstNull(
                value: newName,
                propertyName: nameof(Name));
        }

        #endregion [ Name ]

        #region [ Education ]

        public void SetEducationLevel(EducationLevel newLevel)
        {
            Education = EducationInfo.FromLevel(newLevel);
        }

        public void GraduateTo(EducationLevel newLevel)
        {
            Education = Education.GraduateTo(newLevel);
            ChangeHappiness(PersonHappinessDeltas.OnGraduate);
        }

        #endregion [ Education ]

        #region [ Employment ]

        public void AssignJob(
            DateOnly currentDate,
            Job job)
        {
            Employment = Employment.Change(
                newStatus: EmploymentStatus.Employed,
                newJob: GuardHelper.AgainstNull(
                    value: job,
                    propertyName: nameof(job)),
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

        private void ClearNeedsForDeath()
        {
            Energy = EnergyLevel.From(0);
            Stress = StressLevel.From(0);
            SocialNeed = SocialNeedLevel.From(0);
        }

        #endregion [ Marital ]

        #endregion [ Methods ]
    }
}
