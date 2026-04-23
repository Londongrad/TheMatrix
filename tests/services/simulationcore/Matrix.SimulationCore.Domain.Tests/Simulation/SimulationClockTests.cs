using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.SimulationCore.Domain.Events.Simulation;
using Matrix.SimulationCore.Domain.Scenarios.ClassicCity.Cities;
using Matrix.SimulationCore.Domain.Simulation;
using Xunit;

namespace Matrix.SimulationCore.Domain.Tests.Simulation;

public sealed class SimulationClockTests
{
    private const string RealDeltaNotPositiveErrorCode = "SimulationCore.SimSpeed.RealDelta.NotPositive";
    private static readonly CityId TestCityId = new(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly SimTime TestStartTime = SimTime.FromUtc(
        new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero));
    private static readonly SimTime TestJumpTime = SimTime.FromUtc(
        new DateTimeOffset(2030, 1, 2, 4, 5, 6, TimeSpan.Zero));

    [Fact]
    public void Create_SetsInitialValues_AndEmitsCreatedEvent()
    {
        var speed = SimSpeed.From(60m);

        var clock = SimulationClock.Create(
            cityId: TestCityId,
            startTime: TestStartTime,
            speed: speed,
            initialState: ClockState.Running);

        Assert.Equal(TestStartTime, clock.CurrentTime);
        Assert.Equal(TickId.Start(), clock.TickId);
        Assert.Equal(speed, clock.Speed);
        Assert.Equal(ClockState.Running, clock.State);
        Assert.False(clock.IsPaused);

        var createdEvent = Assert.IsType<SimulationClockCreatedDomainEvent>(Assert.Single(clock.DomainEvents));

        Assert.Equal(clock.SimulationId, createdEvent.SimulationId);
        Assert.Equal(TestCityId, createdEvent.CityId);
        Assert.Equal(TestStartTime, createdEvent.StartTime);
        Assert.Equal(speed, createdEvent.Speed);
        Assert.Equal(ClockState.Running, createdEvent.State);
        Assert.Equal(TickId.Start(), createdEvent.TickId);
    }

    [Fact]
    public void Advance_WhenRunning_AdvancesTime_IncrementsTick_AndEmitsAdvancedEvent()
    {
        var speed = SimSpeed.From(60m);
        var clock = CreateClock(speed: speed);
        var expectedTime = TestStartTime.Add(TimeSpan.FromSeconds(60));

        clock.ClearDomainEvents();

        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(expectedTime, clock.CurrentTime);
        Assert.Equal(new TickId(1), clock.TickId);

        var advancedEvent = Assert.IsType<SimulationTimeAdvancedDomainEvent>(Assert.Single(clock.DomainEvents));

        Assert.Equal(clock.SimulationId, advancedEvent.SimulationId);
        Assert.Equal(TestCityId, advancedEvent.CityId);
        Assert.Equal(TestStartTime, advancedEvent.From);
        Assert.Equal(expectedTime, advancedEvent.To);
        Assert.Equal(new TickId(1), advancedEvent.TickId);
        Assert.Equal(speed, advancedEvent.Speed);
    }

    [Fact]
    public void Advance_WhenPaused_DoesNotChangeState_AndDoesNotEmitEvents()
    {
        var clock = CreateClock(
            initialState: ClockState.Paused,
            speed: SimSpeed.From(60m));

        clock.ClearDomainEvents();

        clock.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(TestStartTime, clock.CurrentTime);
        Assert.Equal(TickId.Start(), clock.TickId);
        Assert.Empty(clock.DomainEvents);
    }

    [Fact]
    public void Advance_WithZeroDelta_ThrowsDomainException()
    {
        var clock = CreateClock();

        var exception = Assert.Throws<DomainException>(() => clock.Advance(TimeSpan.Zero));

        Assert.Equal(RealDeltaNotPositiveErrorCode, exception.Code);
    }

    [Fact]
    public void Advance_WithNegativeDelta_ThrowsDomainException()
    {
        var clock = CreateClock();

        var exception = Assert.Throws<DomainException>(() => clock.Advance(TimeSpan.FromSeconds(-1)));

        Assert.Equal(RealDeltaNotPositiveErrorCode, exception.Code);
    }

    [Fact]
    public void Pause_TransitionsToPaused_EmitsEvent_AndSecondCallIsNoOp()
    {
        var clock = CreateClock();

        clock.ClearDomainEvents();
        clock.Pause();

        Assert.Equal(ClockState.Paused, clock.State);
        Assert.True(clock.IsPaused);
        Assert.Equal(new TickId(1), clock.TickId);

        var pausedEvent = Assert.IsType<SimulationPausedDomainEvent>(Assert.Single(clock.DomainEvents));

        Assert.Equal(clock.SimulationId, pausedEvent.SimulationId);
        Assert.Equal(TestCityId, pausedEvent.CityId);
        Assert.Equal(new TickId(1), pausedEvent.TickId);
        Assert.Equal(TestStartTime, pausedEvent.AtSimTime);

        clock.ClearDomainEvents();
        clock.Pause();

        Assert.Equal(new TickId(1), clock.TickId);
        Assert.Empty(clock.DomainEvents);
    }

    [Fact]
    public void Resume_TransitionsToRunning_EmitsEvent_AndSecondCallIsNoOp()
    {
        var clock = CreateClock(initialState: ClockState.Paused);

        clock.ClearDomainEvents();
        clock.Resume();

        Assert.Equal(ClockState.Running, clock.State);
        Assert.False(clock.IsPaused);
        Assert.Equal(new TickId(1), clock.TickId);

        var resumedEvent = Assert.IsType<SimulationResumedDomainEvent>(Assert.Single(clock.DomainEvents));

        Assert.Equal(clock.SimulationId, resumedEvent.SimulationId);
        Assert.Equal(TestCityId, resumedEvent.CityId);
        Assert.Equal(new TickId(1), resumedEvent.TickId);
        Assert.Equal(TestStartTime, resumedEvent.AtSimTime);

        clock.ClearDomainEvents();
        clock.Resume();

        Assert.Equal(new TickId(1), clock.TickId);
        Assert.Empty(clock.DomainEvents);
    }

    [Fact]
    public void SetSpeed_WithSameSpeed_IsNoOp_AndWithNewSpeed_UpdatesStateAndEmitsEvent()
    {
        var initialSpeed = SimSpeed.From(60m);
        var updatedSpeed = SimSpeed.From(120m);
        var clock = CreateClock(speed: initialSpeed);

        clock.ClearDomainEvents();
        clock.SetSpeed(initialSpeed);

        Assert.Equal(initialSpeed, clock.Speed);
        Assert.Equal(TickId.Start(), clock.TickId);
        Assert.Empty(clock.DomainEvents);

        clock.SetSpeed(updatedSpeed);

        Assert.Equal(updatedSpeed, clock.Speed);
        Assert.Equal(new TickId(1), clock.TickId);

        var speedChangedEvent = Assert.IsType<SimulationSpeedChangedDomainEvent>(Assert.Single(clock.DomainEvents));

        Assert.Equal(clock.SimulationId, speedChangedEvent.SimulationId);
        Assert.Equal(TestCityId, speedChangedEvent.CityId);
        Assert.Equal(new TickId(1), speedChangedEvent.TickId);
        Assert.Equal(initialSpeed, speedChangedEvent.From);
        Assert.Equal(updatedSpeed, speedChangedEvent.To);
        Assert.Equal(TestStartTime, speedChangedEvent.AtSimTime);
    }

    [Fact]
    public void JumpTo_WithSameTime_IsNoOp_AndWithNewTime_UpdatesStateAndEmitsEvent()
    {
        var clock = CreateClock();

        clock.ClearDomainEvents();
        clock.JumpTo(TestStartTime);

        Assert.Equal(TestStartTime, clock.CurrentTime);
        Assert.Equal(TickId.Start(), clock.TickId);
        Assert.Empty(clock.DomainEvents);

        clock.JumpTo(TestJumpTime);

        Assert.Equal(TestJumpTime, clock.CurrentTime);
        Assert.Equal(new TickId(1), clock.TickId);

        var jumpedEvent = Assert.IsType<SimulationTimeJumpedDomainEvent>(Assert.Single(clock.DomainEvents));

        Assert.Equal(clock.SimulationId, jumpedEvent.SimulationId);
        Assert.Equal(TestCityId, jumpedEvent.CityId);
        Assert.Equal(new TickId(1), jumpedEvent.TickId);
        Assert.Equal(TestStartTime, jumpedEvent.From);
        Assert.Equal(TestJumpTime, jumpedEvent.To);
    }

    [Fact]
    public void SimSpeedApply_ScalesAndPreservesRealTimeAsExpected()
    {
        var scaled = SimSpeed.From(60m).Apply(TimeSpan.FromSeconds(1));
        var realTime = SimSpeed.RealTime().Apply(TimeSpan.FromSeconds(5));

        Assert.Equal(TimeSpan.FromSeconds(60), scaled);
        Assert.Equal(TimeSpan.FromSeconds(5), realTime);
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
