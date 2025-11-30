using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;

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
            LifeStateRules.Validate(status, span, health);

            Status = status;
            Span = span;
            Health = health;
        }

        public static LifeState Create(
            LifeStatus status,
            LifeSpan span,
            HealthLevel health)
        {
            return new LifeState(status, span, health);
        }

        public LifeState Change(LifeStatus newStatus, HealthLevel newHealth, DateOnly? newDeathDate)
        {
            var newSpan = LifeSpan.FromDates(
                birthDate: Span.BirthDate,
                deathDate: newDeathDate);

            return new LifeState(newStatus, newSpan, newHealth);
        }

        public LifeState WithHealthDelta(int delta, DateOnly currentDate)
        {
            if (!IsAlive)
                return this;

            var newHealth = Health.WithDelta(delta);

            if (newHealth.Value > 0)
                return Change(Status, newHealth, Span.DeathDate);

            return Change(
                newStatus: LifeStatus.Deceased,
                newHealth: newHealth,
                newDeathDate: currentDate);
        }
    }
}
