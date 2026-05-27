using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.SimulationCore.Domain.Errors;
using Matrix.SimulationCore.Domain.Events.Simulation;

namespace Matrix.SimulationCore.Domain.Simulation
{
    /// <summary>
    ///     Aggregate root that owns simulation time for a city.
    ///     It is deterministic and does not depend on system clock.
    /// </summary>
    public sealed class SimulationClock : AggregateRoot<SimulationId>
    {
        private SimulationClock(
            SimulationId simulationId,
            SimTime currentTime,
            TickId tickId,
            SimSpeed speed,
            ClockState state,
            long pendingSimulationTicks = 0)
            : base(simulationId)
        {
            CurrentTime = currentTime;
            TickId = tickId;
            Speed = speed;
            State = state;
            PendingSimulationTicks = pendingSimulationTicks;
        }

        private SimulationClock()
            : base(default(SimulationId)) { }

        public SimulationId SimulationId => Id;
        public SimTime CurrentTime { get; private set; }
        public TickId TickId { get; private set; }
        public SimSpeed Speed { get; private set; }
        public ClockState State { get; private set; }
        public long PendingSimulationTicks { get; private set; }

        public bool IsPaused => State == ClockState.Paused;
        public TimeSpan PendingSimulationTime => TimeSpan.FromTicks(PendingSimulationTicks);

        public static SimulationClock Create(
            SimulationId simulationId,
            SimTime startTime,
            SimSpeed speed,
            ClockState initialState = ClockState.Running)
        {
            GuardHelper.AgainstEmptyGuid(
                id: simulationId.Value,
                propertyName: nameof(simulationId));

            var clock = new SimulationClock(
                simulationId: simulationId,
                currentTime: startTime,
                tickId: TickId.Start(),
                speed: speed,
                state: initialState);

            clock.AddDomainEvent(
                new SimulationClockCreatedDomainEvent(
                    SimulationId: simulationId,
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
            GuardHelper.Ensure(
                condition: realDelta > TimeSpan.Zero,
                value: realDelta,
                errorFactory: DomainErrorsFactory.SimSpeedRealDeltaMustBePositive);

            if (IsPaused)
                return;

            SimTime from = CurrentTime;
            TimeSpan simDelta = Speed.Apply(realDelta);
            SimTime to = CurrentTime.Add(simDelta);
            SimulationId simulationId = SimulationId;

            TickId = TickId.Next();
            CurrentTime = to;

            AddDomainEvent(
                new SimulationTimeAdvancedDomainEvent(
                    SimulationId: simulationId,
                    From: from,
                    To: to,
                    TickId: TickId,
                    Speed: Speed));
        }

        public void AccumulatePendingSimulationTime(TimeSpan realDelta)
        {
            GuardHelper.Ensure(
                condition: realDelta > TimeSpan.Zero,
                value: realDelta,
                errorFactory: DomainErrorsFactory.SimSpeedRealDeltaMustBePositive);

            if (IsPaused)
                return;

            TimeSpan simDelta = Speed.Apply(realDelta);
            PendingSimulationTicks = checked(PendingSimulationTicks + simDelta.Ticks);
        }

        public bool TryAdvanceFixedStep(TimeSpan fixedStep)
        {
            GuardHelper.Ensure(
                condition: fixedStep > TimeSpan.Zero,
                value: fixedStep,
                errorFactory: DomainErrorsFactory.SimSpeedRealDeltaMustBePositive);

            if (IsPaused)
                return false;

            long fixedStepTicks = fixedStep.Ticks;

            if (PendingSimulationTicks < fixedStepTicks)
                return false;

            SimTime from = CurrentTime;
            SimTime to = CurrentTime.Add(fixedStep);
            SimulationId simulationId = SimulationId;

            PendingSimulationTicks -= fixedStepTicks;
            TickId = TickId.Next();
            CurrentTime = to;

            AddDomainEvent(
                new SimulationTimeAdvancedDomainEvent(
                    SimulationId: simulationId,
                    From: from,
                    To: to,
                    TickId: TickId,
                    Speed: Speed));

            return true;
        }

        public void ClearPendingSimulationTime()
        {
            PendingSimulationTicks = 0;
        }

        public void Pause()
        {
            if (IsPaused)
                return;

            TickId = TickId.Next();
            State = ClockState.Paused;
            SimulationId simulationId = SimulationId;

            AddDomainEvent(
                new SimulationPausedDomainEvent(
                    SimulationId: simulationId,
                    TickId: TickId,
                    AtSimTime: CurrentTime));
        }

        public void Resume()
        {
            if (!IsPaused)
                return;

            TickId = TickId.Next();
            State = ClockState.Running;
            SimulationId simulationId = SimulationId;

            AddDomainEvent(
                new SimulationResumedDomainEvent(
                    SimulationId: simulationId,
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
            SimulationId simulationId = SimulationId;

            AddDomainEvent(
                new SimulationSpeedChangedDomainEvent(
                    SimulationId: simulationId,
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
            ClearPendingSimulationTime();
            SimulationId simulationId = SimulationId;

            AddDomainEvent(
                new SimulationTimeJumpedDomainEvent(
                    SimulationId: simulationId,
                    TickId: TickId,
                    From: from,
                    To: newTime));
        }
    }
}
