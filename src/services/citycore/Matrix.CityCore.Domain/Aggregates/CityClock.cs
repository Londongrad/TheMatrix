using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.CityCore.Domain.Events;
using Matrix.CityCore.Domain.ValueObjects;

namespace Matrix.CityCore.Domain.Aggregates
{
    public sealed class CityClock
    {
        private CityClock() { }

        public CityClock(
            CityClockId id,
            SimulationTime startTime,
            int simMinutesPerTick)
        {
            if (simMinutesPerTick <= 0)
                throw new DomainException(
                    message: "SimMinutesPerTick must be positive.",
                    propertyName: nameof(SimMinutesPerTick));

            Id = id;
            Time = startTime;
            SimMinutesPerTick = simMinutesPerTick;
            IsPaused = false;
        }

        public CityClockId Id { get; private set; }
        public SimulationTime Time { get; private set; } = null!;
        public int SimMinutesPerTick { get; private set; }
        public bool IsPaused { get; private set; }

        public static CityClock CreateDefault()
        {
            // Например, стартуем с 1 января 2050 00:00, 5 минут за тик
            return new CityClock(
                id: CityClockId.New(),
                startTime: new SimulationTime(
                    new DateTime(
                        year: 2050,
                        month: 1,
                        day: 1,
                        hour: 0,
                        minute: 0,
                        second: 0,
                        kind: DateTimeKind.Utc)),
                simMinutesPerTick: 5);
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void ChangeSpeed(int simMinutesPerTick)
        {
            if (simMinutesPerTick <= 0)
                throw new DomainException(
                    message: "SimMinutesPerTick must be positive.",
                    propertyName: nameof(SimMinutesPerTick));

            SimMinutesPerTick = simMinutesPerTick;
        }

        /// <summary>
        ///     Продвигает время на один тик и возвращает доменные события.
        /// </summary>
        public IReadOnlyCollection<ICityDomainEvent> AdvanceOneTick()
        {
            if (IsPaused)
                return Array.Empty<ICityDomainEvent>();

            var events = new List<ICityDomainEvent>();

            DateTime oldTime = Time.Current;
            Time = Time.AddMinutes(SimMinutesPerTick);
            DateTime newTime = Time.Current;

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
                int endedMonth = oldTime.Month;
                int endedYear = oldTime.Year;
                events.Add(
                    new SimulationMonthEnded(
                        Year: endedYear,
                        Month: endedMonth));
            }

            return events;
        }
    }
}
