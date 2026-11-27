# Namespace MicroEngine

<div data-pagefind-weight="0.3">

Basic value types: colours, rectangles, maths and easing helpers.

Guide: [Graphics](../manual/rendering.md)  

### Namespaces

 [MicroEngine.Achievements](MicroEngine.Achievements.md)

 [MicroEngine.Aseprite](MicroEngine.Aseprite.md)

 [MicroEngine.Audio](MicroEngine.Audio.md)

 [MicroEngine.Content](MicroEngine.Content.md)

 [MicroEngine.Core](MicroEngine.Core.md)

 [MicroEngine.Desktop](MicroEngine.Desktop.md)

 [MicroEngine.Ecs](MicroEngine.Ecs.md)

 [MicroEngine.Graphics](MicroEngine.Graphics.md)

 [MicroEngine.Hosting](MicroEngine.Hosting.md)

 [MicroEngine.Input](MicroEngine.Input.md)

 [MicroEngine.Kernel](MicroEngine.Kernel.md)

 [MicroEngine.Localization](MicroEngine.Localization.md)

 [MicroEngine.Navigation](MicroEngine.Navigation.md)

 [MicroEngine.Physics](MicroEngine.Physics.md)

 [MicroEngine.Platform](MicroEngine.Platform.md)

 [MicroEngine.Raylib](MicroEngine.Raylib.md)

 [MicroEngine.Saves](MicroEngine.Saves.md)

 [MicroEngine.Scenes](MicroEngine.Scenes.md)

 [MicroEngine.Sdl](MicroEngine.Sdl.md)

 [MicroEngine.Tiled](MicroEngine.Tiled.md)

 [MicroEngine.UI](MicroEngine.UI.md)

 [MicroEngine.World](MicroEngine.World.md)

### Classes

 [Ease](MicroEngine.Ease.md)

Standard easing curves mapping 0..1 to 0..1 (some overshoot).

 [MathX](MicroEngine.MathX.md)

Small numeric helpers that games repeatedly need for feel and motion.

 [StateMachine<TState, TEvent\>.StateConfig](MicroEngine.StateMachine\-2.StateConfig.md)

The declaration of one state (or of `Any`); every call returns it, to chain them.

 [StateMachine<TState, TEvent\>](MicroEngine.StateMachine\-2.md)

A state machine for gameplay: a character's moves, an enemy's behaviour, a boss's phases, the flow of a level. Each
state has what runs when it is entered, every step while it lasts and when it is left, and the ways out of it: a
condition checked every step (<code>When</code>), a time in the state in seconds (<code>After</code>) or in updates
(<code>AfterSteps</code>) or an event the game fires (<code>On</code>). The machine keeps the seconds and the updates spent in
the current state, runs on whatever step it is given (so it follows a scene's clock through slow motion and a
hitstop), needs no scene (the owner of a rule object calls `Update`), takes part in the state hash and
can be saved and restored. Stepping and firing allocate nothing.

 [StateMachine<TState\>](MicroEngine.StateMachine\-1.md)

A state machine without events: its states change by conditions, by time and by <code>Go</code>.

 [Timers](MicroEngine.Timers.md)

Calls actions after a time or at intervals, counted in seconds or in updates. Whatever owns it calls
[Update](MicroEngine.Timers.md#MicroEngine_Timers_Update_System_Single_) once per step: a scene has one of its own (<code>Scene.Timers</code>), and the rules of a game
kept in a plain class (a stage, a board, a match that tests step without a scene) create theirs. Its state takes
part in the state hash.

 [Track<T\>](MicroEngine.Track\-1.md)

An immutable value that changes over time through keys: `At` gives the value at any time, blended
between the two keys around it. It holds no playhead, so one track can be shared by everything that moves the same
way; a <code>Timeline</code> plays tracks, or call `At` with a time of your own.

 [Track](MicroEngine.Track.md)

Creates the usual kinds of `Track`1`.

### Structs

 [ColorF](MicroEngine.ColorF.md)

A floating-point RGBA colour in linear light, for blending, lighting and anything that adds or scales colours.
Channels are not clamped (values above 1 are brighter than white). [Color](MicroEngine.Color.md) is the 8-bit sRGB form used
to store and name colours; convert with [FromSrgb](MicroEngine.ColorF.md#MicroEngine_ColorF_FromSrgb_MicroEngine_Color_) and [ToSrgb](MicroEngine.ColorF.md#MicroEngine_ColorF_ToSrgb).

 [Color](MicroEngine.Color.md)

An 8-bit-per-channel, non-premultiplied RGBA color.

 [Keyframe<T\>](MicroEngine.Keyframe\-1.md)

One key of a `Track`1`: the value the track has at a time.

 [MachineSnapshot<TState\>](MicroEngine.MachineSnapshot\-1.md)

A machine's state and how long it has been in it, to save and restore (`Restore`).

 [RectF](MicroEngine.RectF.md)

Axis-aligned rectangle in world or screen units (Y grows downward).

 [TimerHandle](MicroEngine.TimerHandle.md)

A timer of a [Timers](MicroEngine.Timers.md); [None](MicroEngine.TimerHandle.md#MicroEngine_TimerHandle_None) names no timer.

### Interfaces

 [IStateMachine](MicroEngine.IStateMachine.md)

Something stepped like a state machine: what a scene's <code>StateMachines</code> system runs.

### Enums

 [NoEvent](MicroEngine.NoEvent.md)

The event type of a state machine that has no events (`StateMachine`1`).

</div>
