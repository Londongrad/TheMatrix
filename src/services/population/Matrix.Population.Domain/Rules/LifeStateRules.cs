using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Errors;
using Matrix.Population.Domain.ValueObjects;

namespace Matrix.Population.Domain.Rules
{
    public static class LifeStateRules
    {
        public static void Validate(
            LifeStatus status,
            LifeSpan span,
            HealthLevel health)
        {
            GuardHelper.AgainstInvalidEnum(status, nameof(status));
            GuardHelper.AgainstNull(span, nameof(span));

            if (status == LifeStatus.Alive)
            {
                if (span.DeathDate is not null)
                    throw DomainErrorsFactory.AlivePersonCannotHaveDeathDate(nameof(status));

                if (health.Value == 0)
                    throw DomainErrorsFactory.AlivePersonCannotHaveZeroHealth(nameof(health));
            }

            if (status == LifeStatus.Deceased)
            {
                if (span.DeathDate is null)
                    throw DomainErrorsFactory.DeceasedPersonMustHaveDeathDate(nameof(status));

                if (health.Value > 0)
                    throw DomainErrorsFactory.DeceasedPersonMustHaveZeroHealth(nameof(health));
            }
        }
    }
}
