using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation
{
    public sealed class SimulationClockTests
    {
        private const string RealDeltaNotPositiveErrorCode = "SimulationCore.SimSpeed.RealDelta.NotPositive";
        private static readonly CityId TestCityId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        private static readonly SimTime TestStartTime = SimTime.FromUtc(
            new DateTimeOffset(
                year: 2030,
                month: 1,
                day: 2,
                hour: 3,
                minute: 4,
                second: 5,
                offset: TimeSpan.Zero));

        private static readonly SimTime TestJumpTime = SimTime.FromUtc(
            new DateTimeOffset(
                year: 2030,
                month: 1,
                day: 2,
                hour: 4,
                minute: 5,
                second: 6,
                offset: TimeSpan.Zero));

        [Fact]
        public void Create_SetsInitialValues_AndEmitsCreatedEvent()
        {
            var speed = SimSpeed.From(60m);

            var clock = SimulationClock.Create(
                cityId: TestCityId,
                startTime: TestStartTime,
                speed: speed,
                initialState: ClockState.Running);

            Assert.Equal(
                expected: TestStartTime,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: TickId.Start(),
                actual: clock.TickId);
            Assert.Equal(
                expected: speed,
                actual: clock.Speed);
            Assert.Equal(
                expected: ClockState.Running,
                actual: clock.State);
            Assert.False(clock.IsPaused);
            Assert.Equal(
                expected: 0,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: TimeSpan.Zero,
                actual: clock.PendingSimulationTime);

            SimulationClockCreatedDomainEvent createdEvent =
                Assert.IsType<SimulationClockCreatedDomainEvent>(Assert.Single(clock.DomainEvents));

            Assert.Equal(
                expected: clock.SimulationId,
                actual: createdEvent.SimulationId);
            Assert.Equal(
                expected: TestCityId,
                actual: createdEvent.CityId);
            Assert.Equal(
                expected: TestStartTime,
                actual: createdEvent.StartTime);
            Assert.Equal(
                expected: speed,
                actual: createdEvent.Speed);
            Assert.Equal(
                expected: ClockState.Running,
                actual: createdEvent.State);
            Assert.Equal(
                expected: TickId.Start(),
                actual: createdEvent.TickId);
        }

        [Fact]
        public void Advance_WhenRunning_AdvancesTime_IncrementsTick_AndEmitsAdvancedEvent()
        {
            var speed = SimSpeed.From(60m);
            SimulationClock clock = CreateClock(speed: speed);
            SimTime expectedTime = TestStartTime.Add(TimeSpan.FromSeconds(60));

            clock.ClearDomainEvents();

            clock.Advance(TimeSpan.FromSeconds(1));

            Assert.Equal(
                expected: expectedTime,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: new TickId(1),
                actual: clock.TickId);

            SimulationTimeAdvancedDomainEvent advancedEvent =
                Assert.IsType<SimulationTimeAdvancedDomainEvent>(Assert.Single(clock.DomainEvents));

            Assert.Equal(
                expected: clock.SimulationId,
                actual: advancedEvent.SimulationId);
            Assert.Equal(
                expected: TestCityId,
                actual: advancedEvent.CityId);
            Assert.Equal(
                expected: TestStartTime,
                actual: advancedEvent.From);
            Assert.Equal(
                expected: expectedTime,
                actual: advancedEvent.To);
            Assert.Equal(
                expected: new TickId(1),
                actual: advancedEvent.TickId);
            Assert.Equal(
                expected: speed,
                actual: advancedEvent.Speed);
        }

        [Fact]
        public void Advance_WhenPaused_DoesNotChangeState_AndDoesNotEmitEvents()
        {
            SimulationClock clock = CreateClock(
                initialState: ClockState.Paused,
                speed: SimSpeed.From(60m));

            clock.ClearDomainEvents();

            clock.Advance(TimeSpan.FromSeconds(1));

            Assert.Equal(
                expected: TestStartTime,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: TickId.Start(),
                actual: clock.TickId);
            Assert.Empty(clock.DomainEvents);
        }

        [Fact]
        public void Advance_WithZeroDelta_ThrowsDomainException()
        {
            SimulationClock clock = CreateClock();

            DomainException exception = Assert.Throws<DomainException>(() => clock.Advance(TimeSpan.Zero));

            Assert.Equal(
                expected: RealDeltaNotPositiveErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void Advance_WithNegativeDelta_ThrowsDomainException()
        {
            SimulationClock clock = CreateClock();

            DomainException exception = Assert.Throws<DomainException>(() => clock.Advance(TimeSpan.FromSeconds(-1)));

            Assert.Equal(
                expected: RealDeltaNotPositiveErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void AccumulatePendingSimulationTime_WhenRunning_AccumulatesScaledSimulationTimeWithoutAdvancingClock()
        {
            var speed = SimSpeed.From(60m);
            SimulationClock clock = CreateClock(speed: speed);

            clock.ClearDomainEvents();

            clock.AccumulatePendingSimulationTime(TimeSpan.FromSeconds(1));

            Assert.Equal(
                expected: TimeSpan.FromSeconds(60)
                   .Ticks,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(60),
                actual: clock.PendingSimulationTime);
            Assert.Equal(
                expected: TestStartTime,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: TickId.Start(),
                actual: clock.TickId);
            Assert.Empty(clock.DomainEvents);
        }

        [Fact]
        public void AccumulatePendingSimulationTime_WhenPaused_DoesNotChangeStateOrEmitEvents()
        {
            SimulationClock clock = CreateClock(speed: SimSpeed.From(60m));

            clock.ClearDomainEvents();
            clock.AccumulatePendingSimulationTime(TimeSpan.FromSeconds(1));
            clock.Pause();
            clock.ClearDomainEvents();

            long pendingTicksBefore = clock.PendingSimulationTicks;
            SimTime currentTimeBefore = clock.CurrentTime;
            TickId tickIdBefore = clock.TickId;

            clock.AccumulatePendingSimulationTime(TimeSpan.FromSeconds(1));

            Assert.Equal(
                expected: pendingTicksBefore,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: currentTimeBefore,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: tickIdBefore,
                actual: clock.TickId);
            Assert.Empty(clock.DomainEvents);
        }

        [Fact]
        public void AccumulatePendingSimulationTime_WithZeroDelta_ThrowsDomainException()
        {
            SimulationClock clock = CreateClock();

            DomainException exception =
                Assert.Throws<DomainException>(() => clock.AccumulatePendingSimulationTime(TimeSpan.Zero));

            Assert.Equal(
                expected: RealDeltaNotPositiveErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void TryAdvanceFixedStep_WithInsufficientBacklog_ReturnsFalseWithoutChangingState()
        {
            SimulationClock clock = CreateClock(speed: SimSpeed.From(60m));

            clock.ClearDomainEvents();
            clock.AccumulatePendingSimulationTime(TimeSpan.FromSeconds(1));
            clock.ClearDomainEvents();

            bool advanced = clock.TryAdvanceFixedStep(TimeSpan.FromSeconds(61));

            Assert.False(advanced);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(60)
                   .Ticks,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: TestStartTime,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: TickId.Start(),
                actual: clock.TickId);
            Assert.Empty(clock.DomainEvents);
        }

        [Fact]
        public void TryAdvanceFixedStep_WithExactBacklog_AdvancesOneStepAndEmitsAdvancedEvent()
        {
            var speed = SimSpeed.From(60m);
            SimulationClock clock = CreateClock(speed: speed);
            SimTime expectedTime = TestStartTime.Add(TimeSpan.FromSeconds(60));

            clock.ClearDomainEvents();
            clock.AccumulatePendingSimulationTime(TimeSpan.FromSeconds(1));
            clock.ClearDomainEvents();

            bool advanced = clock.TryAdvanceFixedStep(TimeSpan.FromSeconds(60));

            Assert.True(advanced);
            Assert.Equal(
                expected: 0,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: TimeSpan.Zero,
                actual: clock.PendingSimulationTime);
            Assert.Equal(
                expected: expectedTime,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: new TickId(1),
                actual: clock.TickId);

            SimulationTimeAdvancedDomainEvent advancedEvent =
                Assert.IsType<SimulationTimeAdvancedDomainEvent>(Assert.Single(clock.DomainEvents));

            Assert.Equal(
                expected: clock.SimulationId,
                actual: advancedEvent.SimulationId);
            Assert.Equal(
                expected: TestCityId,
                actual: advancedEvent.CityId);
            Assert.Equal(
                expected: TestStartTime,
                actual: advancedEvent.From);
            Assert.Equal(
                expected: expectedTime,
                actual: advancedEvent.To);
            Assert.Equal(
                expected: new TickId(1),
                actual: advancedEvent.TickId);
            Assert.Equal(
                expected: speed,
                actual: advancedEvent.Speed);
        }

        [Fact]
        public void TryAdvanceFixedStep_WithBacklogRemainder_LeavesRemainingPendingSimulationTime()
        {
            SimulationClock clock = CreateClock(speed: SimSpeed.From(130m));

            clock.ClearDomainEvents();
            clock.AccumulatePendingSimulationTime(TimeSpan.FromSeconds(1));
            clock.ClearDomainEvents();

            bool advanced = clock.TryAdvanceFixedStep(TimeSpan.FromSeconds(60));

            Assert.True(advanced);
            Assert.Equal(
                expected: TestStartTime.Add(TimeSpan.FromSeconds(60)),
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(70)
                   .Ticks,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(70),
                actual: clock.PendingSimulationTime);
        }

        [Fact]
        public void TryAdvanceFixedStep_WhenPaused_ReturnsFalseWithoutChangingState()
        {
            SimulationClock clock = CreateClock(speed: SimSpeed.From(60m));

            clock.ClearDomainEvents();
            clock.AccumulatePendingSimulationTime(TimeSpan.FromSeconds(1));
            clock.Pause();
            clock.ClearDomainEvents();

            long pendingTicksBefore = clock.PendingSimulationTicks;
            SimTime currentTimeBefore = clock.CurrentTime;
            TickId tickIdBefore = clock.TickId;

            bool advanced = clock.TryAdvanceFixedStep(TimeSpan.FromSeconds(60));

            Assert.False(advanced);
            Assert.Equal(
                expected: pendingTicksBefore,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: currentTimeBefore,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: tickIdBefore,
                actual: clock.TickId);
            Assert.Empty(clock.DomainEvents);
        }

        [Fact]
        public void TryAdvanceFixedStep_WithZeroStep_ThrowsDomainException()
        {
            SimulationClock clock = CreateClock();

            DomainException exception = Assert.Throws<DomainException>(() => clock.TryAdvanceFixedStep(TimeSpan.Zero));

            Assert.Equal(
                expected: RealDeltaNotPositiveErrorCode,
                actual: exception.Code);
        }

        [Fact]
        public void ClearPendingSimulationTime_ClearsBacklogWithoutAdvancingOrEmittingEvents()
        {
            SimulationClock clock = CreateClock(speed: SimSpeed.From(60m));

            clock.ClearDomainEvents();
            clock.AccumulatePendingSimulationTime(TimeSpan.FromSeconds(1));
            clock.ClearDomainEvents();

            clock.ClearPendingSimulationTime();

            Assert.Equal(
                expected: 0,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: TimeSpan.Zero,
                actual: clock.PendingSimulationTime);
            Assert.Equal(
                expected: TestStartTime,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: TickId.Start(),
                actual: clock.TickId);
            Assert.Empty(clock.DomainEvents);
        }

        [Fact]
        public void PauseResumeAndSetSpeed_PreservePendingSimulationTime()
        {
            SimulationClock clock = CreateClock(speed: SimSpeed.From(60m));

            clock.ClearDomainEvents();
            clock.AccumulatePendingSimulationTime(TimeSpan.FromSeconds(1));
            long pendingTicksBefore = clock.PendingSimulationTicks;

            clock.Pause();
            clock.Resume();
            clock.SetSpeed(SimSpeed.From(120m));

            Assert.Equal(
                expected: pendingTicksBefore,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(60),
                actual: clock.PendingSimulationTime);
        }

        [Fact]
        public void Pause_TransitionsToPaused_EmitsEvent_AndSecondCallIsNoOp()
        {
            SimulationClock clock = CreateClock();

            clock.ClearDomainEvents();
            clock.Pause();

            Assert.Equal(
                expected: ClockState.Paused,
                actual: clock.State);
            Assert.True(clock.IsPaused);
            Assert.Equal(
                expected: new TickId(1),
                actual: clock.TickId);

            SimulationPausedDomainEvent pausedEvent =
                Assert.IsType<SimulationPausedDomainEvent>(Assert.Single(clock.DomainEvents));

            Assert.Equal(
                expected: clock.SimulationId,
                actual: pausedEvent.SimulationId);
            Assert.Equal(
                expected: TestCityId,
                actual: pausedEvent.CityId);
            Assert.Equal(
                expected: new TickId(1),
                actual: pausedEvent.TickId);
            Assert.Equal(
                expected: TestStartTime,
                actual: pausedEvent.AtSimTime);

            clock.ClearDomainEvents();
            clock.Pause();

            Assert.Equal(
                expected: new TickId(1),
                actual: clock.TickId);
            Assert.Empty(clock.DomainEvents);
        }

        [Fact]
        public void Resume_TransitionsToRunning_EmitsEvent_AndSecondCallIsNoOp()
        {
            SimulationClock clock = CreateClock(initialState: ClockState.Paused);

            clock.ClearDomainEvents();
            clock.Resume();

            Assert.Equal(
                expected: ClockState.Running,
                actual: clock.State);
            Assert.False(clock.IsPaused);
            Assert.Equal(
                expected: new TickId(1),
                actual: clock.TickId);

            SimulationResumedDomainEvent resumedEvent =
                Assert.IsType<SimulationResumedDomainEvent>(Assert.Single(clock.DomainEvents));

            Assert.Equal(
                expected: clock.SimulationId,
                actual: resumedEvent.SimulationId);
            Assert.Equal(
                expected: TestCityId,
                actual: resumedEvent.CityId);
            Assert.Equal(
                expected: new TickId(1),
                actual: resumedEvent.TickId);
            Assert.Equal(
                expected: TestStartTime,
                actual: resumedEvent.AtSimTime);

            clock.ClearDomainEvents();
            clock.Resume();

            Assert.Equal(
                expected: new TickId(1),
                actual: clock.TickId);
            Assert.Empty(clock.DomainEvents);
        }

        [Fact]
        public void SetSpeed_WithSameSpeed_IsNoOp_AndWithNewSpeed_UpdatesStateAndEmitsEvent()
        {
            var initialSpeed = SimSpeed.From(60m);
            var updatedSpeed = SimSpeed.From(120m);
            SimulationClock clock = CreateClock(speed: initialSpeed);

            clock.ClearDomainEvents();
            clock.SetSpeed(initialSpeed);

            Assert.Equal(
                expected: initialSpeed,
                actual: clock.Speed);
            Assert.Equal(
                expected: TickId.Start(),
                actual: clock.TickId);
            Assert.Empty(clock.DomainEvents);

            clock.SetSpeed(updatedSpeed);

            Assert.Equal(
                expected: updatedSpeed,
                actual: clock.Speed);
            Assert.Equal(
                expected: new TickId(1),
                actual: clock.TickId);

            SimulationSpeedChangedDomainEvent speedChangedEvent =
                Assert.IsType<SimulationSpeedChangedDomainEvent>(Assert.Single(clock.DomainEvents));

            Assert.Equal(
                expected: clock.SimulationId,
                actual: speedChangedEvent.SimulationId);
            Assert.Equal(
                expected: TestCityId,
                actual: speedChangedEvent.CityId);
            Assert.Equal(
                expected: new TickId(1),
                actual: speedChangedEvent.TickId);
            Assert.Equal(
                expected: initialSpeed,
                actual: speedChangedEvent.From);
            Assert.Equal(
                expected: updatedSpeed,
                actual: speedChangedEvent.To);
            Assert.Equal(
                expected: TestStartTime,
                actual: speedChangedEvent.AtSimTime);
        }

        [Fact]
        public void JumpTo_WithSameTime_IsNoOp_AndWithNewTime_UpdatesStateAndEmitsEvent()
        {
            SimulationClock clock = CreateClock();

            clock.ClearDomainEvents();
            clock.JumpTo(TestStartTime);

            Assert.Equal(
                expected: TestStartTime,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: TickId.Start(),
                actual: clock.TickId);
            Assert.Empty(clock.DomainEvents);

            clock.JumpTo(TestJumpTime);

            Assert.Equal(
                expected: TestJumpTime,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: new TickId(1),
                actual: clock.TickId);

            SimulationTimeJumpedDomainEvent jumpedEvent =
                Assert.IsType<SimulationTimeJumpedDomainEvent>(Assert.Single(clock.DomainEvents));

            Assert.Equal(
                expected: clock.SimulationId,
                actual: jumpedEvent.SimulationId);
            Assert.Equal(
                expected: TestCityId,
                actual: jumpedEvent.CityId);
            Assert.Equal(
                expected: new TickId(1),
                actual: jumpedEvent.TickId);
            Assert.Equal(
                expected: TestStartTime,
                actual: jumpedEvent.From);
            Assert.Equal(
                expected: TestJumpTime,
                actual: jumpedEvent.To);
        }

        [Fact]
        public void JumpTo_WhenTimeChanges_ClearsPendingSimulationTime()
        {
            SimulationClock clock = CreateClock(speed: SimSpeed.From(60m));

            clock.ClearDomainEvents();
            clock.AccumulatePendingSimulationTime(TimeSpan.FromSeconds(1));

            Assert.NotEqual(
                expected: 0,
                actual: clock.PendingSimulationTicks);

            clock.ClearDomainEvents();
            clock.JumpTo(TestJumpTime);

            Assert.Equal(
                expected: TestJumpTime,
                actual: clock.CurrentTime);
            Assert.Equal(
                expected: 0,
                actual: clock.PendingSimulationTicks);
            Assert.Equal(
                expected: TimeSpan.Zero,
                actual: clock.PendingSimulationTime);
            Assert.Equal(
                expected: new TickId(1),
                actual: clock.TickId);

            SimulationTimeJumpedDomainEvent jumpedEvent =
                Assert.IsType<SimulationTimeJumpedDomainEvent>(Assert.Single(clock.DomainEvents));

            Assert.Equal(
                expected: clock.SimulationId,
                actual: jumpedEvent.SimulationId);
            Assert.Equal(
                expected: TestCityId,
                actual: jumpedEvent.CityId);
            Assert.Equal(
                expected: new TickId(1),
                actual: jumpedEvent.TickId);
            Assert.Equal(
                expected: TestStartTime,
                actual: jumpedEvent.From);
            Assert.Equal(
                expected: TestJumpTime,
                actual: jumpedEvent.To);
        }

        [Fact]
        public void SimSpeedApply_ScalesAndPreservesRealTimeAsExpected()
        {
            TimeSpan scaled = SimSpeed.From(60m)
               .Apply(TimeSpan.FromSeconds(1));
            TimeSpan realTime = SimSpeed.RealTime()
               .Apply(TimeSpan.FromSeconds(5));

            Assert.Equal(
                expected: TimeSpan.FromSeconds(60),
                actual: scaled);
            Assert.Equal(
                expected: TimeSpan.FromSeconds(5),
                actual: realTime);
        }

        private static SimulationClock CreateClock(
            ClockState initialState = ClockState.Running,
            SimSpeed? speed = null,
            SimTime? startTime = null)
        {
            return SimulationClock.Create(
                cityId: TestCityId,
                startTime: startTime ?? TestStartTime,
                speed: speed ?? SimSpeed.RealTime(),
                initialState: initialState);
        }
    }
}
