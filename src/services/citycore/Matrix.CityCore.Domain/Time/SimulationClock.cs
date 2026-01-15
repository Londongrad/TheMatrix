using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.CityCore.Domain.Common;
using Matrix.CityCore.Domain.Enums;
using Matrix.CityCore.Domain.Events;

namespace Matrix.CityCore.Domain.Time
{
    /// <summary>
    ///     Aggregate root that owns simulation time for a city.
    ///     It is deterministic and does not depend on system clock.
    /// </summary>
    public sealed class SimulationClock : AggregateRoot<CityId>
    {
        private SimulationClock(
            CityId cityId,
            SimTime currentTime,
            TickId tickId,
            SimSpeed speed,
            ClockState state)
            : base(cityId)
        {
            CurrentTime = currentTime;
            TickId = tickId;
            Speed = speed;
            State = state;
        }

        public SimTime CurrentTime { get; private set; }
        public TickId TickId { get; private set; }
        public SimSpeed Speed { get; private set; }
        public ClockState State { get; private set; }

        public bool IsPaused => State == ClockState.Paused;

        public static SimulationClock Create(
            CityId cityId,
            SimTime startTime,
            SimSpeed speed,
            ClockState initialState = ClockState.Running)
        {
            var clock = new SimulationClock(
                cityId: cityId,
                currentTime: startTime,
                tickId: TickId.Start(),
                speed: speed,
                state: initialState);

            clock.AddDomainEvent(
                new SimulationClockCreatedDomainEvent(
                    CityId: cityId,
                    StartTime: startTime,
                    Speed: speed,
                    State: initialState,
                    TickId: clock.TickId));

            return clock;
        }

        /// <summary>
        ///     Advances simulation time using a real-world delta (provided by application layer).
        ///     If paused, no changes are applied.
        /// </summary>
        public void Advance(TimeSpan realDelta)
        {
            if (realDelta <= TimeSpan.Zero)
                throw new DomainException("realDelta must be positive.");

            if (IsPaused)
                return;

            SimTime from = CurrentTime;
            TimeSpan simDelta = Speed.Apply(realDelta);
            SimTime to = CurrentTime.Add(simDelta);

            TickId = TickId.Next();
            CurrentTime = to;

            AddDomainEvent(
                new SimulationTimeAdvancedDomainEvent(
                    CityId: Id,
                    From: from,
                    To: to,
                    TickId: TickId,
                    Speed: Speed));
        }

        public void Pause()
        {
            if (IsPaused)
                return;

            TickId = TickId.Next();
            State = ClockState.Paused;

            AddDomainEvent(
                new SimulationPausedDomainEvent(
                    CityId: Id,
                    TickId: TickId,
                    AtSimTime: CurrentTime));
        }

        public void Resume()
        {
            if (!IsPaused)
                return;

            TickId = TickId.Next();
            State = ClockState.Running;

            AddDomainEvent(
                new SimulationResumedDomainEvent(
                    CityId: Id,
                    TickId: TickId,
                    AtSimTime: CurrentTime));
        }

        public void SetSpeed(SimSpeed newSpeed)
        {
            if (newSpeed.Equals(Speed))
                return;

            SimSpeed from = Speed;

            TickId = TickId.Next();
            Speed = newSpeed;

            AddDomainEvent(
                new SimulationSpeedChangedDomainEvent(
                    CityId: Id,
                    TickId: TickId,
                    From: from,
                    To: newSpeed,
                    AtSimTime: CurrentTime));
        }

        /// <summary>
        ///     Jumps simulation time to an exact value (admin/debug feature, catch-up scenarios).
        /// </summary>
        public void JumpTo(SimTime newTime)
        {
            if (newTime.Equals(CurrentTime))
                return;

            SimTime from = CurrentTime;

            TickId = TickId.Next();
            CurrentTime = newTime;

            AddDomainEvent(
                new SimulationTimeJumpedDomainEvent(
                    CityId: Id,
                    TickId: TickId,
                    From: from,
                    To: newTime));
        }
    }
}
