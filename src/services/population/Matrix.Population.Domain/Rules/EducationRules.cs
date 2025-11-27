using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Errors;

namespace Matrix.Population.Domain.Rules
{
    public static class EducationRules
    {
        public static void ValidateTransition(EducationLevel from, EducationLevel to)
        {
            GuardHelper.AgainstInvalidEnum(from, nameof(from));
            GuardHelper.AgainstInvalidEnum(to, nameof(to));

            // Нет фактического изменения — всегда ок.
            if (from == to)
                return;

            // Downgrade — нельзя.
            if (to < from)
                throw PopulationErrors.CannotDowngradeEducation(from, to);

            switch (from)
            {
                case EducationLevel.None:
                    if (to != EducationLevel.Preschool)
                        throw PopulationErrors.InvalidEducationTransition(from, to);
                    break;

                case EducationLevel.Preschool:
                    if (to != EducationLevel.Primary)
                        throw PopulationErrors.InvalidEducationTransition(from, to);
                    break;

                case EducationLevel.Primary:
                    if (to != EducationLevel.LowerSecondary)
                        throw PopulationErrors.InvalidEducationTransition(from, to);
                    break;

                case EducationLevel.LowerSecondary:
                    if (to != EducationLevel.UpperSecondary)
                        throw PopulationErrors.InvalidEducationTransition(from, to);
                    break;

                case EducationLevel.UpperSecondary:
                    if (to is not (EducationLevel.Vocational or EducationLevel.Higher))
                        throw PopulationErrors.InvalidEducationTransition(from, to);
                    break;

                case EducationLevel.Vocational:
                    if (to != EducationLevel.Higher)
                        throw PopulationErrors.InvalidEducationTransition(from, to);
                    break;

                case EducationLevel.Higher:
                    if (to != EducationLevel.Postgraduate)
                        throw PopulationErrors.InvalidEducationTransition(from, to);
                    break;

                case EducationLevel.Postgraduate:
                    // Уже потолок — дальше нельзя.
                    throw PopulationErrors.InvalidEducationTransition(from, to);

                default:
                    throw PopulationErrors.InvalidEducationTransition(from, to);
            }
        }
    }
}
