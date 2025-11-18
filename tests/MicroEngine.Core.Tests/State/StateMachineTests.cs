using MicroEngine.Core.State;

namespace MicroEngine.Core.Tests.State;

public sealed class StateMachineTests
{
    private enum TestState
    {
        Idle,
        Walking,
        Running,
        Jumping,
        Falling,
        Dead
    }

    private enum TestTrigger
    {
        Walk,
        Run,
        Jump,
        Land,
        Fall,
        Die,
        Respawn
    }

    [Fact]
    public void Constructor_SetsInitialState()
    {
        // Act
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);

        // Assert
        Assert.Equal(TestState.Idle, sm.CurrentState);
    }

    [Fact]
    public void Fire_WithPermittedTrigger_TransitionsToTargetState()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        sm.Configure(TestState.Idle)
            .Permit(TestTrigger.Walk, TestState.Walking);

        // Act
        var result = sm.Fire(TestTrigger.Walk);

        // Assert
        Assert.True(result);
        Assert.Equal(TestState.Walking, sm.CurrentState);
    }

    [Fact]
    public void Fire_WithUnpermittedTrigger_DoesNotTransition()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        sm.Configure(TestState.Idle)
            .Permit(TestTrigger.Walk, TestState.Walking);

        // Act
        var result = sm.Fire(TestTrigger.Jump);

        // Assert
        Assert.False(result);
        Assert.Equal(TestState.Idle, sm.CurrentState);
    }

    [Fact]
    public void FireStrict_WithPermittedTrigger_TransitionsSuccessfully()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        sm.Configure(TestState.Idle)
            .Permit(TestTrigger.Walk, TestState.Walking);

        // Act
        sm.FireStrict(TestTrigger.Walk);

        // Assert
        Assert.Equal(TestState.Walking, sm.CurrentState);
    }

    [Fact]
    public void FireStrict_WithUnpermittedTrigger_ThrowsInvalidOperationException()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => sm.FireStrict(TestTrigger.Jump));
        Assert.Contains("No transition defined", exception.Message);
        Assert.Contains("Idle", exception.Message);
        Assert.Contains("Jump", exception.Message);
    }

    [Fact]
    public void CanFire_WithPermittedTrigger_ReturnsTrue()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        sm.Configure(TestState.Idle)
            .Permit(TestTrigger.Walk, TestState.Walking);

        // Act
        var result = sm.CanFire(TestTrigger.Walk);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanFire_WithUnpermittedTrigger_ReturnsFalse()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        sm.Configure(TestState.Idle)
            .Permit(TestTrigger.Walk, TestState.Walking);

        // Act
        var result = sm.CanFire(TestTrigger.Jump);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Configure_SupportsMultiplePermits()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        sm.Configure(TestState.Idle)
            .Permit(TestTrigger.Walk, TestState.Walking)
            .Permit(TestTrigger.Run, TestState.Running)
            .Permit(TestTrigger.Jump, TestState.Jumping);

        // Act & Assert
        Assert.True(sm.CanFire(TestTrigger.Walk));
        Assert.True(sm.CanFire(TestTrigger.Run));
        Assert.True(sm.CanFire(TestTrigger.Jump));
    }

    [Fact]
    public void PermitIf_WithGuardTrue_AllowsTransition()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        var canRun = true;
        sm.Configure(TestState.Idle)
            .PermitIf(TestTrigger.Run, TestState.Running, () => canRun);

        // Act
        var result = sm.Fire(TestTrigger.Run);

        // Assert
        Assert.True(result);
        Assert.Equal(TestState.Running, sm.CurrentState);
    }

    [Fact]
    public void PermitIf_WithGuardFalse_PreventsTransition()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        var canRun = false;
        sm.Configure(TestState.Idle)
            .PermitIf(TestTrigger.Run, TestState.Running, () => canRun);

        // Act
        var result = sm.Fire(TestTrigger.Run);

        // Assert
        Assert.False(result);
        Assert.Equal(TestState.Idle, sm.CurrentState);
    }

    [Fact]
    public void PermitIf_WithNullGuard_ThrowsArgumentNullException()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            sm.Configure(TestState.Idle).PermitIf(TestTrigger.Run, TestState.Running, null!));
    }

    [Fact]
    public void OnEntry_ExecutesActionWhenEnteringState()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        var entryExecuted = false;
        
        sm.Configure(TestState.Walking)
            .OnEntry(() => entryExecuted = true);
        
        sm.Configure(TestState.Idle)
            .Permit(TestTrigger.Walk, TestState.Walking);

        // Act
        sm.Fire(TestTrigger.Walk);

        // Assert
        Assert.True(entryExecuted);
    }

    [Fact]
    public void OnExit_ExecutesActionWhenExitingState()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        var exitExecuted = false;
        
        sm.Configure(TestState.Idle)
            .OnExit(() => exitExecuted = true)
            .Permit(TestTrigger.Walk, TestState.Walking);

        // Act
        sm.Fire(TestTrigger.Walk);

        // Assert
        Assert.True(exitExecuted);
    }

    [Fact]
    public void OnEntry_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            sm.Configure(TestState.Idle).OnEntry(null!));
    }

    [Fact]
    public void OnExit_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            sm.Configure(TestState.Idle).OnExit(null!));
    }

    [Fact]
    public void Transition_ExecutesExitThenEntry()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        var executionOrder = new List<string>();
        
        sm.Configure(TestState.Idle)
            .OnExit(() => executionOrder.Add("Exit Idle"))
            .Permit(TestTrigger.Walk, TestState.Walking);
        
        sm.Configure(TestState.Walking)
            .OnEntry(() => executionOrder.Add("Enter Walking"));

        // Act
        sm.Fire(TestTrigger.Walk);

        // Assert
        Assert.Equal(2, executionOrder.Count);
        Assert.Equal("Exit Idle", executionOrder[0]);
        Assert.Equal("Enter Walking", executionOrder[1]);
    }

    [Fact]
    public void Ignore_PreventsTriggerFromCausingTransition()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        sm.Configure(TestState.Idle)
            .Ignore(TestTrigger.Jump);

        // Act
        var canFire = sm.CanFire(TestTrigger.Jump);
        var result = sm.Fire(TestTrigger.Jump);

        // Assert
        Assert.False(canFire);
        Assert.False(result);
        Assert.Equal(TestState.Idle, sm.CurrentState);
    }

    [Fact]
    public void Reset_ReturnsToInitialState()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        sm.Configure(TestState.Idle)
            .Permit(TestTrigger.Walk, TestState.Walking);
        
        sm.Configure(TestState.Walking)
            .Permit(TestTrigger.Run, TestState.Running);

        sm.Fire(TestTrigger.Walk);
        sm.Fire(TestTrigger.Run);
        Assert.Equal(TestState.Running, sm.CurrentState);

        // Act
        sm.Reset();

        // Assert
        Assert.Equal(TestState.Idle, sm.CurrentState);
    }

    [Fact]
    public void Reset_WhenAlreadyAtInitialState_DoesNothing()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        var entryCount = 0;
        
        sm.Configure(TestState.Idle)
            .OnEntry(() => entryCount++);

        // Act
        sm.Reset();

        // Assert
        Assert.Equal(TestState.Idle, sm.CurrentState);
        Assert.Equal(0, entryCount); // Entry should not be called
    }

    [Fact]
    public void Reset_ExecutesExitAndEntryActions()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        var executionOrder = new List<string>();
        
        sm.Configure(TestState.Idle)
            .OnEntry(() => executionOrder.Add("Enter Idle"))
            .Permit(TestTrigger.Walk, TestState.Walking);
        
        sm.Configure(TestState.Walking)
            .OnExit(() => executionOrder.Add("Exit Walking"));

        sm.Fire(TestTrigger.Walk);
        executionOrder.Clear();

        // Act
        sm.Reset();

        // Assert
        Assert.Equal(2, executionOrder.Count);
        Assert.Equal("Exit Walking", executionOrder[0]);
        Assert.Equal("Enter Idle", executionOrder[1]);
    }

    [Fact]
    public void StateMachine_ComplexWorkflow_WorksCorrectly()
    {
        // Arrange - Create a character state machine
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        
        sm.Configure(TestState.Idle)
            .Permit(TestTrigger.Walk, TestState.Walking)
            .Permit(TestTrigger.Run, TestState.Running)
            .Permit(TestTrigger.Jump, TestState.Jumping)
            .Permit(TestTrigger.Die, TestState.Dead);
        
        sm.Configure(TestState.Walking)
            .Permit(TestTrigger.Run, TestState.Running)
            .Permit(TestTrigger.Jump, TestState.Jumping)
            .Permit(TestTrigger.Die, TestState.Dead);
        
        sm.Configure(TestState.Running)
            .Permit(TestTrigger.Walk, TestState.Walking)
            .Permit(TestTrigger.Jump, TestState.Jumping)
            .Permit(TestTrigger.Die, TestState.Dead);
        
        sm.Configure(TestState.Jumping)
            .Permit(TestTrigger.Land, TestState.Idle)
            .Permit(TestTrigger.Fall, TestState.Falling);
        
        sm.Configure(TestState.Falling)
            .Permit(TestTrigger.Land, TestState.Idle)
            .Permit(TestTrigger.Die, TestState.Dead);
        
        sm.Configure(TestState.Dead)
            .Permit(TestTrigger.Respawn, TestState.Idle);

        // Act - Simulate gameplay
        sm.FireStrict(TestTrigger.Walk);
        Assert.Equal(TestState.Walking, sm.CurrentState);
        
        sm.FireStrict(TestTrigger.Run);
        Assert.Equal(TestState.Running, sm.CurrentState);
        
        sm.FireStrict(TestTrigger.Jump);
        Assert.Equal(TestState.Jumping, sm.CurrentState);
        
        sm.FireStrict(TestTrigger.Fall);
        Assert.Equal(TestState.Falling, sm.CurrentState);
        
        sm.FireStrict(TestTrigger.Die);
        Assert.Equal(TestState.Dead, sm.CurrentState);
        
        sm.FireStrict(TestTrigger.Respawn);
        Assert.Equal(TestState.Idle, sm.CurrentState);
    }

    [Fact]
    public void StateMachine_WithGuardedTransitions_OnlyAllowsWhenConditionsMet()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        var stamina = 100;
        
        sm.Configure(TestState.Idle)
            .PermitIf(TestTrigger.Run, TestState.Running, () => stamina >= 50)
            .Permit(TestTrigger.Walk, TestState.Walking);

        // Act & Assert - Can run with enough stamina
        Assert.True(sm.CanFire(TestTrigger.Run));
        sm.FireStrict(TestTrigger.Run);
        Assert.Equal(TestState.Running, sm.CurrentState);

        sm.Reset();
        stamina = 25;

        // Cannot run with low stamina
        Assert.False(sm.CanFire(TestTrigger.Run));
        Assert.False(sm.Fire(TestTrigger.Run));
        Assert.Equal(TestState.Idle, sm.CurrentState);

        // But can still walk
        Assert.True(sm.Fire(TestTrigger.Walk));
        Assert.Equal(TestState.Walking, sm.CurrentState);
    }

    [Fact]
    public void StateMachine_EntryAndExitActions_TrackStateLifecycle()
    {
        // Arrange
        var sm = new StateMachine<TestState, TestTrigger>(TestState.Idle);
        var log = new List<string>();
        
        sm.Configure(TestState.Idle)
            .OnEntry(() => log.Add("Idle: Ready"))
            .OnExit(() => log.Add("Idle: Starting movement"))
            .Permit(TestTrigger.Walk, TestState.Walking);
        
        sm.Configure(TestState.Walking)
            .OnEntry(() => log.Add("Walking: Moving"))
            .OnExit(() => log.Add("Walking: Speeding up"))
            .Permit(TestTrigger.Run, TestState.Running);
        
        sm.Configure(TestState.Running)
            .OnEntry(() => log.Add("Running: At full speed"));

        // Act
        sm.FireStrict(TestTrigger.Walk);
        sm.FireStrict(TestTrigger.Run);

        // Assert - Initial state doesn't trigger OnEntry, only transitions do
        Assert.Equal(4, log.Count);
        Assert.Equal("Idle: Starting movement", log[0]);
        Assert.Equal("Walking: Moving", log[1]);
        Assert.Equal("Walking: Speeding up", log[2]);
        Assert.Equal("Running: At full speed", log[3]);
    }
}
