using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Errors
{
    public static class PopulationErrors
    {
        #region [ Person ]

        public static DomainException StudentCannotBeChild(string? propertyName = null) =>
            new(
                code: "Population.Person.StudentCannotBeChild",
                message: "Student must not be a child.",
                propertyName: propertyName);

        public static DomainException SeniorMustBeInRetiredAgeGroup(string? propertyName = null) =>
            new(
                code: "Population.Person.SeniorMustBeInRetiredAgeGroup",
                message: "Senior person must be in retired age group.",
                propertyName: propertyName);

        public static DomainException DeceasedPersonCannotBeEmployed(string? propertyName = null) =>
            new(
                code: "Population.Person.DeceasedPersonCannotBeEmployed",
                message: "Deceased person cannot be employed.",
                propertyName: propertyName);

        public static DomainException OnlyAdultsCanBeEmployed(string? propertyName = null) =>
            new(
                code: "Population.Person.OnlyAdultsCanBeEmployed",
                message: "Only adults can be employed.",
                propertyName: propertyName);

        public static DomainException DeceasedPersonCannotBeFired(string? propertyName = null) =>
            new(
                code: "Population.Person.DeceasedPersonCannotBeFired",
                message: "Deceased person cannot be fired.",
                propertyName: propertyName);

        public static DomainException UnemployedPersonCannotBeFired(string? propertyName = null) =>
            new(
                code: "Population.Person.UnemployedPersonCannotBeFired",
                message: "Unemployed person cannot be fired.",
                propertyName: propertyName);

        public static DomainException DeceasedPersonCannotRetire(string? propertyName = null) =>
            new(
                code: "Population.Person.DeceasedPersonCannotRetire",
                message: "Deceased person cannot retire.",
                propertyName: propertyName);

        public static DomainException OnlySeniorsCanRetire(string? propertyName = null) =>
            new(
                code: "Population.Person.OnlySeniorsCanRetire",
                message: "Only seniors can retire.",
                propertyName: propertyName);

        public static DomainException JobRequiredWhenChangingStatusToEmployed(string? propertyName = null) =>
            new(
                code: "Population.Person.JobRequiredWhenChangingStatusToEmployed",
                message: "Job must be provided when changing status to Employed.",
                propertyName: propertyName);

        public static DomainException ChildOrSeniorCannotBeStudent(string? propertyName = null) =>
            new(
                code: "Population.Person.ChildOrSeniorCannotBeStudent",
                message: "Child/Senior cannot be a student.",
                propertyName: propertyName);

        public static DomainException DeceasedPersonCannotMarry(string? propertyName = null) =>
            new(
                code: "Population.Person.DeceasedPersonCannotMarry",
                message: "Deceased person cannot marry.",
                propertyName: propertyName);

        public static DomainException PersonAlreadyMarried(string? propertyName = null) =>
            new(
                code: "Population.Person.AlreadyMarried",
                message: "Person is already married.",
                propertyName: propertyName);

        public static DomainException DeceasedPersonCannotDivorce(string? propertyName = null) =>
            new(
                code: "Population.Person.DeceasedPersonCannotDivorce",
                message: "Deceased person cannot divorce.",
                propertyName: propertyName);

        public static DomainException PersonNotMarried(string? propertyName = null) =>
            new(
                code: "Population.Person.NotMarried",
                message: "Person is not married.",
                propertyName: propertyName);

        public static DomainException DeceasedPersonCannotBecomeWidow(string? propertyName = null) =>
            new(
                code: "Population.Person.DeceasedPersonCannotBecomeWidow",
                message: "Deceased person cannot become a widow(er).",
                propertyName: propertyName);

        public static DomainException PersonAlreadyDead(string? propertyName = null) =>
            new(
                code: "Population.Person.AlreadyDead",
                message: "Person is already dead.",
                propertyName: propertyName);

        public static DomainException PersonAlreadyAlive(string? propertyName = null) =>
            new(
                code: "Population.Person.AlreadyAlive",
                message: "Person is already alive.",
                propertyName: propertyName);

        public static DomainException EnsureConsistencyForDeadCalledForAlivePerson(string? propertyName = null) =>
            new(
                code: "Population.Person.EnsureConsistencyForDeadCalledForAlivePerson",
                message: "EnsureConsistencyForDead called for an alive person.",
                propertyName: propertyName);

        public static DomainException DeceasedPersonMustHaveDeathDate(string? propertyName = null) =>
            new(
                code: "Population.Person.DeceasedPersonMustHaveDeathDate",
                message: "Deceased person must have a death date.",
                propertyName: propertyName);

        public static DomainException DeceasedPersonEmploymentStatusMustBeNone(string? propertyName = null) =>
            new(
                code: "Population.Person.DeceasedPersonEmploymentStatusMustBeNone",
                message: "Deceased person should have employmentStatus = None.",
                propertyName: propertyName);

        public static DomainException DeceasedPersonCannotHaveJob(string? propertyName = null) =>
            new(
                code: "Population.Person.DeceasedPersonCannotHaveJob",
                message: "Deceased person cannot have a job.",
                propertyName: propertyName);

        public static DomainException DeceasedPersonMustHaveZeroHealth(string? propertyName = null) =>
            new(
                code: "Population.Person.DeceasedPersonMustHaveZeroHealth",
                message: "Deceased person must have zero health.",
                propertyName: propertyName);

        public static DomainException AlivePersonCannotHaveDeathDate(string? propertyName = null) =>
            new(
                code: "Population.Person.AlivePersonCannotHaveDeathDate",
                message: "Alive person cannot have a death date.",
                propertyName: propertyName);

        public static DomainException ChildCannotBeEmployed(string? propertyName = null) =>
            new(
                code: "Population.Person.ChildCannotBeEmployed",
                message: "Child cannot be employed.",
                propertyName: propertyName);

        public static DomainException RetiredPersonCannotBeEmployedOrStudent(string? propertyName = null) =>
            new(
                code: "Population.Person.RetiredPersonCannotBeEmployedOrStudent",
                message: "Retired person cannot be employed or a student.",
                propertyName: propertyName);

        public static DomainException EmployedPersonMustHaveJob(string? propertyName = null) =>
            new(
                code: "Population.Person.EmployedPersonMustHaveJob",
                message: "Employed person must have a job.",
                propertyName: propertyName);

        public static DomainException OnlyEmployedPersonCanHaveJob(string? propertyName = null) =>
            new(
                code: "Population.Person.OnlyEmployedPersonCanHaveJob",
                message: "Only employed person can have a job.",
                propertyName: propertyName);

        public static DomainException AlivePersonCannotHaveZeroHealth(string? propertyName = null) =>
            new(
                code: "Population.Person.AlivePersonWithZeroHealth",
                message: "Alive person cannot have zero health.",
                propertyName: propertyName);

        public static DomainException DeathCannotBeEarlierThenBirth(string? propertyName = null) =>
            new(
                code: "Population.Person.DeathCannotBeEarlierThenBirth",
                message: "Death cannot be earlier then birth.",
                propertyName: propertyName);

        public static DomainException MaritalStatusCannotHaveSpouseId(string? propertyName = null) =>
            new(
                code: "Population.Person.MaritalStatusCannotHaveSpouseId",
                message: "Marital status cannot have a spouse ID.",
                propertyName: propertyName);

        public static DomainException MarriedMustHaveSpouseId(string? propertyName = null) =>
            new(
                code: "Population.Person.MarriedMustHaveSpouseId",
                message: "Married status must have a spouse ID.",
                propertyName: propertyName);

        public static DomainException CannotDowngradeEducation(
            EducationLevel from,
            EducationLevel to)
            => new(
                code: "Population.Person.Education.CannotDowngrade",
                message: $"Cannot change education from '{from}' to lower level '{to}'.",
                propertyName: nameof(EducationLevel));

        public static DomainException InvalidEducationTransition(
            EducationLevel from,
            EducationLevel to)
            => new(
                code: "Population.Person.Education.InvalidTransition",
                message: $"Invalid education transition from '{from}' to '{to}'.",
                propertyName: nameof(EducationLevel));

        public static DomainException CannotMarrySelf(PersonId personId)
            => new(
                code: "Population.Person.Marital.CannotMarrySelf",
                message: $"Person '{personId}' cannot marry themselves.");

        public static DomainException PersonTooYoungToMarry(PersonId personId, int ageYears)
            => new(
                code: "Population.Person.Marital.PersonTooYoung",
                message: $"Person '{personId}' is too young to marry (age: {ageYears}).");

        public static DomainException SpouseTooYoungToMarry(PersonId spouseId, int ageYears)
            => new(
                code: "Population.Person.Marital.SpouseTooYoung",
                message: $"Spouse '{spouseId}' is too young to marry (age: {ageYears}).");

        public static DomainException PersonAlreadyMarried(PersonId personId)
            => new(
                code: "Population.Person.Marital.PersonAlreadyMarried",
                message: $"Person '{personId}' is already married.");

        public static DomainException SpouseAlreadyMarried(PersonId spouseId)
            => new(
                code: "Population.Person.Marital.SpouseAlreadyMarried",
                message: $"Spouse '{spouseId}' is already married.");

        public static DomainException CurrentDateLessThanBirth()
            => new(
                code: "Population.Person.CurrentDateLessThanBirth",
                message: "Current date cannot be less than birth date.");

        public static DomainException AgeIncrementMustBePositive(string? propertyName = null)
            => new(
                code: "Population.Person.Age.AgeIncrementMustBePositive",
                message: "Age increment must be positive.",
                propertyName: propertyName);

        public static DomainException AgeCannotExceedMaxYears(int maxYears, string? propertyName = null)
            => new(
                code: "Population.Person.Age.AgeCannotExceedMaxYears",
                message: $"Age cannot exceed {maxYears} years.",
                propertyName: propertyName);

        #endregion [ Person ]
    }
}
