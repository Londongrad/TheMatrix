using Matrix.BuildingBlocks.Domain;
using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Errors;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class LifeState
    {
        public LifeStatus Status { get; }
        public LifeSpan Span { get; } = null!;
        public HealthLevel Health { get; }

        public bool IsAlive => Status == LifeStatus.Alive;
        public DateOnly BirthDate => Span.BirthDate;
        public DateOnly? DeathDate => Span.DeathDate;

        private LifeState() { }
        private LifeState(LifeStatus status, LifeSpan span, HealthLevel health)
        {
            Status = GuardHelper.AgainstInvalidEnum(status, nameof(status));
            Span = GuardHelper.AgainstNull(span, nameof(span));
            Health = health;

            ValidateCombination(Status, Span, Health);
        }

        public static LifeState Create(
            LifeStatus status,
            LifeSpan span,
            HealthLevel health)
        {
            return new LifeState(status, span, health);
        }

        /// <summary>
        /// Универсальное изменение жизненного состояния.
        /// Снаружи ты говоришь, на что поменять, а здесь проверяются все инварианты.
        /// </summary>
        public LifeState Change(
            LifeStatus newStatus,
            HealthLevel newHealth,
            DateOnly? newDeathDate)
        {
            var newSpan = LifeSpan.FromDates(
                birthDate: Span.BirthDate,
                deathDate: newDeathDate);

            ValidateCombination(newStatus, newSpan, newHealth);

            return new LifeState(newStatus, newSpan, newHealth);
        }

        /// <summary>
        /// Меняем здоровье. Если упало до нуля, автоматически переходим в Deceased.
        /// </summary>
        public LifeState WithHealthDelta(int delta, DateOnly currentDate)
        {
            if (!IsAlive)
                return this;

            var newHealth = Health.WithDelta(delta);

            if (newHealth.Value > 0)
            {
                return Change(Status, newHealth, Span.DeathDate);
            }

            // здоровье стало 0 → статус Deceased + фиксируем дату смерти
            return Change(
                newStatus: LifeStatus.Deceased,
                newHealth: newHealth,
                newDeathDate: currentDate);
        }

        private static void ValidateCombination(
            LifeStatus status,
            LifeSpan span,
            HealthLevel health)
        {
            if (status == LifeStatus.Alive)
            {
                if (span.DeathDate is not null)
                    throw PopulationErrors.AlivePersonCannotHaveDeathDate(nameof(status));

                if (health.Value == 0)
                    throw PopulationErrors.AlivePersonCannotHaveZeroHealth(nameof(health));
            }

            if (status == LifeStatus.Deceased)
            {
                if (span.DeathDate is null)
                    throw PopulationErrors.DeceasedPersonMustHaveDeathDate(nameof(status));

                if (health.Value > 0)
                    throw PopulationErrors.DeceasedPersonMustHaveZeroHealth(nameof(health));
            }
        }
    }
}
