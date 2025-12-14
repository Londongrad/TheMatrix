using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Errors
{
    public static class DomainErrorsFactory
    {
        #region [ Person - Name ]

        public static DomainException InvalidFullName(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Name.InvalidFullName",
                message: "Full name must consist of first name, last name, and optional patronymic.",
                propertyName: propertyName);
        }

        #endregion [ Person - Name ]

        #region [ Person - Life ]

        public static DomainException PersonAlreadyDead(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Life.PersonAlreadyDead",
                message: "Person is already dead.",
                propertyName: propertyName);
        }

        public static DomainException PersonAlreadyAlive(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Life.PersonAlreadyAlive",
                message: "Person is already alive.",
                propertyName: propertyName);
        }

        public static DomainException EnsureConsistencyForDeadCalledForAlivePerson(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Life.EnsureConsistencyForDeadCalledForAlivePerson",
                message: "EnsureConsistencyForDead called for an alive person.",
                propertyName: propertyName);
        }

        public static DomainException DeceasedPersonMustHaveDeathDate(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Life.DeceasedPersonMustHaveDeathDate",
                message: "Deceased person must have a death date.",
                propertyName: propertyName);
        }

        public static DomainException AlivePersonCannotHaveDeathDate(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Life.AlivePersonCannotHaveDeathDate",
                message: "Alive person cannot have a death date.",
                propertyName: propertyName);
        }

        public static DomainException DeathCannotBeEarlierThenBirth(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Age.DeathCannotBeEarlierThenBirth",
                message: "Death cannot be earlier then birth.",
                propertyName: propertyName);
        }

        public static DomainException CurrentDateLessThanBirth(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Age.CurrentDateLessThanBirth",
                message: "Current date cannot be less than birth date.",
                propertyName: propertyName);
        }

        #endregion [ Person - Life ]

        #region [ Person - Health ]

        public static DomainException DeceasedPersonMustHaveZeroHealth(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Health.DeceasedPersonMustHaveZeroHealth",
                message: "Deceased person must have zero health.",
                propertyName: propertyName);
        }

        public static DomainException AlivePersonCannotHaveZeroHealth(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Health.AlivePersonCannotHaveZeroHealth",
                message: "Alive person cannot have zero health.",
                propertyName: propertyName);
        }

        #endregion [ Person - Health ]

        #region [ Person - Employment ]

        public static DomainException DeceasedPersonCannotBeEmployed(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.DeceasedPersonCannotBeEmployed",
                message: "Deceased person cannot be employed.",
                propertyName: propertyName);
        }

        public static DomainException OnlyAdultsCanBeEmployed(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.OnlyAdultsCanBeEmployed",
                message: "Only adults can be employed.",
                propertyName: propertyName);
        }

        public static DomainException DeceasedPersonCannotBeFired(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.DeceasedPersonCannotBeFired",
                message: "Deceased person cannot be fired.",
                propertyName: propertyName);
        }

        public static DomainException UnemployedPersonCannotBeFired(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.UnemployedPersonCannotBeFired",
                message: "Unemployed person cannot be fired.",
                propertyName: propertyName);
        }

        public static DomainException DeceasedPersonCannotRetire(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.DeceasedPersonCannotRetire",
                message: "Deceased person cannot retire.",
                propertyName: propertyName);
        }

        public static DomainException OnlySeniorsCanRetire(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.OnlySeniorsCanRetire",
                message: "Only seniors can retire.",
                propertyName: propertyName);
        }

        public static DomainException JobRequiredWhenChangingStatusToEmployed(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.JobRequiredWhenChangingStatusToEmployed",
                message: "Job must be provided when changing status to Employed.",
                propertyName: propertyName);
        }

        public static DomainException ChildCannotBeEmployed(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.ChildCannotBeEmployed",
                message: "Child cannot be employed.",
                propertyName: propertyName);
        }

        public static DomainException RetiredPersonCannotBeEmployedOrStudent(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.RetiredPersonCannotBeEmployedOrStudent",
                message: "Retired person cannot be employed or a student.",
                propertyName: propertyName);
        }

        public static DomainException EmployedPersonMustHaveJob(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.EmployedPersonMustHaveJob",
                message: "Employed person must have a job.",
                propertyName: propertyName);
        }

        public static DomainException OnlyEmployedPersonCanHaveJob(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.OnlyEmployedPersonCanHaveJob",
                message: "Only employed person can have a job.",
                propertyName: propertyName);
        }

        public static DomainException DeceasedPersonEmploymentStatusMustBeNone(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.DeceasedPersonEmploymentStatusMustBeNone",
                message: "Deceased person should have employmentStatus = None.",
                propertyName: propertyName);
        }

        public static DomainException DeceasedPersonCannotHaveJob(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Employment.DeceasedPersonCannotHaveJob",
                message: "Deceased person cannot have a job.",
                propertyName: propertyName);
        }

        #endregion [ Person - Employment ]

        #region [ Person - Education ]

        public static DomainException StudentCannotBeChild(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Education.StudentCannotBeChild",
                message: "Student must not be a child.",
                propertyName: propertyName);
        }

        public static DomainException SeniorMustBeInRetiredAgeGroup(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Age.SeniorMustBeInRetiredAgeGroup",
                message: "Senior person must be in retired age group.",
                propertyName: propertyName);
        }

        public static DomainException ChildOrSeniorCannotBeStudent(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Education.ChildOrSeniorCannotBeStudent",
                message: "Child/Senior cannot be a student.",
                propertyName: propertyName);
        }

        public static DomainException CannotDowngradeEducation(
            EducationLevel from,
            EducationLevel to)
        {
            return new DomainException(
                code: "Population.Person.Education.CannotDowngrade",
                message: $"Cannot change education from '{from}' to lower level '{to}'.",
                propertyName: nameof(EducationLevel));
        }

        public static DomainException InvalidEducationTransition(
            EducationLevel from,
            EducationLevel to)
        {
            return new DomainException(
                code: "Population.Person.Education.InvalidTransition",
                message: $"Invalid education transition from '{from}' to '{to}'.",
                propertyName: nameof(EducationLevel));
        }

        #endregion [ Person - Education ]

        #region [ Person - Marital ]

        public static DomainException DeceasedPersonCannotMarry(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.DeceasedPersonCannotMarry",
                message: "Deceased person cannot marry.",
                propertyName: propertyName);
        }

        public static DomainException PersonAlreadyMarried(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.PersonAlreadyMarried",
                message: "Person is already married.",
                propertyName: propertyName);
        }

        public static DomainException DeceasedPersonCannotDivorce(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.DeceasedPersonCannotDivorce",
                message: "Deceased person cannot divorce.",
                propertyName: propertyName);
        }

        public static DomainException PersonNotMarried(
            PersonId personId,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.PersonNotMarried",
                message: $"Person {personId} is not married.",
                propertyName: propertyName);
        }

        public static DomainException SpouseNotMarried(
            PersonId personId,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.SpouseNotMarried",
                message: $"Spouse {personId} is not married.",
                propertyName: propertyName);
        }

        public static DomainException DeceasedPersonCannotBecomeWidow(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.DeceasedPersonCannotBecomeWidow",
                message: "Deceased person cannot become a widow(er).",
                propertyName: propertyName);
        }

        public static DomainException MaritalStatusCannotHaveSpouseId(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.MaritalStatusCannotHaveSpouseId",
                message: "Marital status cannot have a spouse ID.",
                propertyName: propertyName);
        }

        public static DomainException MarriedMustHaveSpouseId(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.MarriedMustHaveSpouseId",
                message: "Married status must have a spouse ID.",
                propertyName: propertyName);
        }

        public static DomainException CannotMarrySelf(
            PersonId personId,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.CannotMarrySelf",
                message: $"Person '{personId}' cannot marry themselves.",
                propertyName: propertyName);
        }

        public static DomainException CannotDivorceSelf(
            PersonId personId,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.CannotDivorceSelf",
                message: $"Person '{personId}' cannot divorce themselves.",
                propertyName: propertyName);
        }

        public static DomainException PersonTooYoungToMarry(
            PersonId personId,
            int ageYears,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.PersonTooYoung",
                message: $"Person '{personId}' is too young to marry (age: {ageYears}).",
                propertyName: propertyName);
        }

        public static DomainException SpouseTooYoungToMarry(
            PersonId spouseId,
            int ageYears,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.SpouseTooYoung",
                message: $"Spouse '{spouseId}' is too young to marry (age: {ageYears}).",
                propertyName: propertyName);
        }

        public static DomainException PersonAlreadyMarried(
            PersonId personId,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.PersonAlreadyMarried",
                message: $"Person '{personId}' is already married.",
                propertyName: propertyName);
        }

        public static DomainException SpouseAlreadyMarried(
            PersonId spouseId,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.SpouseAlreadyMarried",
                message: $"Spouse '{spouseId}' is already married.",
                propertyName: propertyName);
        }

        public static DomainException CannotBecomeWidowedOfSelf(
            PersonId widowId,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.CannotBecomeWidowedOfSelf",
                message: $"Person '{widowId}' cannot become widowed of themselves.",
                propertyName: propertyName);
        }

        public static DomainException WidowMustBeAlive(
            PersonId widowId,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.WidowMustBeAlive",
                message: $"Person '{widowId}' must be alive to become widowed.",
                propertyName: propertyName);
        }

        public static DomainException WidowMustBeMarried(
            PersonId widowId,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.WidowMustBeMarried",
                message: $"Person '{widowId}' must be married to become widowed.",
                propertyName: propertyName);
        }

        public static DomainException DeceasedMustBeDeceased(
            PersonId deceasedId,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.DeceasedMustBeDeceased",
                message: $"Person '{deceasedId}' must be deceased to make someone widowed.",
                propertyName: propertyName);
        }

        public static DomainException DeceasedMustBeMarried(
            PersonId deceasedId,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Marital.DeceasedMustBeMarried",
                message: $"Person '{deceasedId}' must be married to the widow(er).",
                propertyName: propertyName);
        }

        #endregion [ Person - Marital ]

        #region [ Person - Age ]

        public static DomainException AgeIncrementMustBePositive(string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Age.AgeIncrementMustBePositive",
                message: "Age increment must be positive.",
                propertyName: propertyName);
        }

        public static DomainException AgeCannotExceedMaxYears(
            int maxYears,
            string? propertyName = null)
        {
            return new DomainException(
                code: "Population.Person.Age.AgeCannotExceedMaxYears",
                message: $"Age cannot exceed {maxYears} years.",
                propertyName: propertyName);
        }

        #endregion [ Person - Age ]
    }
}
