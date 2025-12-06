using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Errors;

namespace Matrix.Population.Domain.Rules
{
    public static class EducationRules
    {
        public static void ValidateTransition(EducationLevel from, EducationLevel to)
        {
            GuardHelper.AgainstInvalidEnum(value: from, propertyName: nameof(from));
            GuardHelper.AgainstInvalidEnum(value: to, propertyName: nameof(to));

            // Нет фактического изменения — всегда ок.
            if (from == to)
                return;

            // Downgrade — нельзя.
            if (to < from)
                throw DomainErrorsFactory.CannotDowngradeEducation(from: from, to: to);

            switch (from)
            {
                case EducationLevel.None:
                    if (to != EducationLevel.Preschool)
                        throw DomainErrorsFactory.InvalidEducationTransition(from: from, to: to);
                    break;

                case EducationLevel.Preschool:
                    if (to != EducationLevel.Primary)
                        throw DomainErrorsFactory.InvalidEducationTransition(from: from, to: to);
                    break;

                case EducationLevel.Primary:
                    if (to != EducationLevel.LowerSecondary)
                        throw DomainErrorsFactory.InvalidEducationTransition(from: from, to: to);
                    break;

                case EducationLevel.LowerSecondary:
                    if (to != EducationLevel.UpperSecondary)
                        throw DomainErrorsFactory.InvalidEducationTransition(from: from, to: to);
                    break;

                case EducationLevel.UpperSecondary:
                    if (to is not (EducationLevel.Vocational or EducationLevel.Higher))
                        throw DomainErrorsFactory.InvalidEducationTransition(from: from, to: to);
                    break;

                case EducationLevel.Vocational:
                    if (to != EducationLevel.Higher)
                        throw DomainErrorsFactory.InvalidEducationTransition(from: from, to: to);
                    break;

                case EducationLevel.Higher:
                    if (to != EducationLevel.Postgraduate)
                        throw DomainErrorsFactory.InvalidEducationTransition(from: from, to: to);
                    break;

                case EducationLevel.Postgraduate:
                    // Уже потолок — дальше нельзя.
                    throw DomainErrorsFactory.InvalidEducationTransition(from: from, to: to);

                default:
                    throw DomainErrorsFactory.InvalidEducationTransition(from: from, to: to);
            }
        }
    }
}
