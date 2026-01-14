using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.CityCore.Domain.Events;
using Matrix.CityCore.Domain.ValueObjects;

namespace Matrix.CityCore.Domain.Aggregates
{
    public sealed class CityClock
    {
        public CityClockId Id { get; private set; }
        public SimulationTime Time { get; private set; } = null!;
        public int SimMinutesPerTick { get; private set; }
        public bool IsPaused { get; private set; }

        private CityClock()
        {
        }

        public CityClock(CityClockId id, SimulationTime startTime, int simMinutesPerTick)
        {
            if (simMinutesPerTick <= 0)
                throw new DomainException("SimMinutesPerTick must be positive.", nameof(SimMinutesPerTick));

            Id = id;
            Time = startTime;
            SimMinutesPerTick = simMinutesPerTick;
            IsPaused = false;
        }

        public static CityClock CreateDefault()
        {
            // Например, стартуем с 1 января 2050 00:00, 5 минут за тик
            return new CityClock(
                CityClockId.New(),
                new SimulationTime(new DateTime(2050, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                simMinutesPerTick: 5);
        }

        public void Pause() => IsPaused = true;

        public void Resume() => IsPaused = false;

        public void ChangeSpeed(int simMinutesPerTick)
        {
            if (simMinutesPerTick <= 0)
                throw new DomainException("SimMinutesPerTick must be positive.", nameof(SimMinutesPerTick));

            SimMinutesPerTick = simMinutesPerTick;
        }

        /// <summary>
        /// Продвигает время на один тик и возвращает доменные события.
        /// </summary>
        public IReadOnlyCollection<ICityDomainEvent> AdvanceOneTick()
        {
            if (IsPaused)
                return Array.Empty<ICityDomainEvent>();

            var events = new List<ICityDomainEvent>();

            var oldTime = Time.Current;
            Time = Time.AddMinutes(SimMinutesPerTick);
            var newTime = Time.Current;

            // Всегда публикуем факт продвижения времени
            events.Add(new SimulationTimeAdvanced(newTime));

            // Смена дня
            if (oldTime.Date != newTime.Date)
            {
                var endedDay = DateOnly.FromDateTime(oldTime.Date);
                events.Add(new SimulationDayEnded(endedDay));
            }

            // Смена месяца
            if (oldTime.Month != newTime.Month || oldTime.Year != newTime.Year)
            {
                var endedMonth = oldTime.Month;
                var endedYear = oldTime.Year;
                events.Add(new SimulationMonthEnded(endedYear, endedMonth));
            }

            return events;
        }
    }
}
