using Matrix.Population.Domain.Enums;
using Matrix.Population.Domain.Rules;

namespace Matrix.Population.Domain.ValueObjects
{
    public sealed class LifeState
    {
        private LifeState()
        {
        }

        private LifeState(LifeStatus status, LifeSpan span, HealthLevel health)
        {
            LifeStateRules.Validate(status: status, span: span, health: health);

            Status = status;
            Span = span;
            Health = health;
        }

        public LifeStatus Status { get; }
        public LifeSpan Span { get; } = null!;
        public HealthLevel Health { get; }

        public bool IsAlive => Status == LifeStatus.Alive;
        public DateOnly BirthDate => Span.BirthDate;
        public DateOnly? DeathDate => Span.DeathDate;

        public static LifeState Create(LifeStatus status, LifeSpan span, HealthLevel health) =>
            new(status: status, span: span, health: health);

        public LifeState Change(LifeStatus newStatus, HealthLevel newHealth, DateOnly? newDeathDate)
        {
            var newSpan = LifeSpan.FromDates(
                birthDate: Span.BirthDate,
                deathDate: newDeathDate);

            return new LifeState(status: newStatus, span: newSpan, health: newHealth);
        }

        public LifeState WithHealthDelta(int delta, DateOnly currentDate)
        {
            if (!IsAlive)
                return this;

            HealthLevel newHealth = Health.WithDelta(delta);

            if (newHealth.Value > 0)
                return Change(newStatus: Status, newHealth: newHealth, newDeathDate: Span.DeathDate);

            return Change(
                newStatus: LifeStatus.Deceased,
                newHealth: newHealth,
                newDeathDate: currentDate);
        }
    }
}
