# GAPS-CHARACTER: a character controller a classic platformer can use

Evaluation of 2026-11-26 for NastMz/MicroEngine#218, group B, on `main` at a4cbe289. Method of
[CONSOLIDATION-1](consolidation-1.md), section 1. Decision record:
[ADR 0075](../decisions/0075-character-feel-in-one-motion.md). The measurements it starts from are in
`odd/tasks/demo-modern-1/meadow-run.md`, `neon-rift.md` and `courtyard-key.md`.

No time was measured: nothing here is decided by a frame time. Every figure is a count of steps compared exactly.

## A. The need

A side-view platform game with a feel of its own, and a top-down game with actors placed by their feet. Three of the
four demos are such games and none could adopt the controllers:

- Meadow Run writes its mover (`Player.Step`, 200 lines over `TileMotion`): the controller differs from it in 66 of
  94,285 steps with a record swapped per state, 1921 with the two records the sample showed.
- Neon Rift writes its hop: a released jump is cut to 45 % of its speed at once.
- Courtyard Key keeps `TileMotion` for its actors: a push needs the side that stopped the box, its actors stand on
  their feet, its walkers and flyers see different tiles, and the controller's positions differed in their last bits.

## B. Today

`CharacterMotion` had one speed, one share for the air, and the bonus of a run-up measured against the current
speed. A game that runs swapped the record; a game with air momentum built a record per air step. `Launch` could
not be cut, `Drop` was a level, the jump buffer was private, `Teleport` cleared the ground, there was no way to keep
or set the state, and a platform with a rule of the game's own could not be ground.

## C. Alternatives looked at

None of these was run for this evaluation; the first and the third are in the repository.

| Alternative | What it is | Verdict |
| --- | --- | --- |
| Keep the game's mover | The documented path: `TileMotion` and 200 lines | What every demo did. It is the cost this issue measures |
| Godot 4 `CharacterBody2D` (MIT), from its documentation | `move_and_slide`, floor snap, platform velocity inheritance (`platform_on_leave`), no jump, no gravity, no coyote time | The model for "what is carried when a character leaves a platform" as a setting. Its feel is written by every game in script; the engine here keeps the feel as data |
| Box2D 3.2 mover (`b2World_CastMover`, `b2SolvePlanes`; MIT), the one in the native layer | Geometry only | Already under `Move` on the Box2D backend. Rules stay above the contract |
| The rules platform games publish about themselves (the `Player` class of Celeste, released by its authors; the write-ups of coyote time, jump buffering, corner correction and apex gravity) | Rules, not a library | The vocabulary: the parameters are named after what those rules do for the player. No code was taken |
| A hook in the middle of the step | A callback between input and move | Declined: the two orders games have fit an enum, and a callback would run while the controller holds a move's result |
| A parameter per quirk of each demo | Eight flags | Declined: see D |

## D. Target experience

The game code first. Meadow Run's Pip, as a developer would write him:

```csharp
static readonly CharacterMotion Pip = new()
{
    MoveSpeed = 92, Acceleration = 520, RunSpeed = 152, RunAcceleration = 600,
    Deceleration = 620, OverspeedDeceleration = 620, SkidDeceleration = 1500, SkidSpeed = 30,
    AirAcceleration = 430, AirDeceleration = 140, AirOverspeedDeceleration = 0,
    JumpSpeed = 302, RunJumpBonus = 26, Gravity = 640, /* release, apex and fall scales */
    CoyoteTime = 0.1f, JumpBuffer = 0.12f, LandingReach = 1, CornerCorrection = 4, CeilingReboundSpeed = 20,
    DropThroughTime = 0.22f, DropWithJump = true, Order = CharacterStepOrder.ActThenCarry,
};

hero.Step(dt, new CharacterInput(axis, jumpPressed, jumpHeld, Drop: down && !riding, Run: run));
if (hitHiddenBlock) hero.Velocity = hero.Velocity with { Y = 0 };           // the game's rule
if (platform.Catches(hero)) hero.StandOn(platform.Shape, onTopOfIt);       // the game's rule
```

That is the mapping of `tests/MeadowRun.Tests/ControllerShadow.cs`. The stomp is
`hero.Launch(velocity, cuttable: held)`, the camera clamp `hero.Position = clamped`, the autopilot's look-ahead
`Snapshot`, steps, `Restore`.

The gaps of the issue and the piece that closes each:

| Gap | Piece | Why this and not a flag |
| --- | --- | --- |
| Landing reach | `LandingReach` | A distance, any game's |
| Reference speed of the bonus | The bonus is measured against the motion's top speed (`RunSpeed` or `MoveSpeed`) | No new number: the run is in the motion now |
| Air acceleration and drag | `AirAcceleration`, `AirDeceleration` | Two numbers every platformer tunes |
| Air momentum, friction above the cap | `AirOverspeedDeceleration`, `OverspeedDeceleration` | One idea (speed above what is asked) with a rate for each medium; 0 in the air is momentum |
| Order of carry, jump and drop | `Order` | Both orders exist in games; it also puts a drop's first step in the air |
| Buffered drop | `DropWithJump` | "Down and jump" is a control scheme; the drop takes the jump, buffered or pressed |
| A launch that can be cut | `Launch(velocity, cuttable)` | |
| State copied and set | `Snapshot`, `Restore`, `CharacterState`, settable `Position` | |
| Jump cut to a share at once (Neon Rift) | `JumpReleaseSpeedScale` | Beside the gravity scale, the other classic way |
| The side that stopped the box (Courtyard Key) | `MoveContact.Stopped`, `MoveResult.StoppedGoing` | Names the shape too, which the helper's four flags do not |
| Actors placed by their feet | `offset` in both `Create` | The body's origin is the game's; no second coordinate to keep in the sync |
| Walkers and flyers | `grid.Blocking(flags)` | Views of one grid cannot fall out of step; no backend changes |
| Platform landing rule of the game | `StandOn` | The rule stays the game's |

## E. Responsibility limit

The controller owns the rules a second game would have. It does not own a game's rule for one kind of ground, what
a hidden block does to the head, or damage: those are written after the step with what the step reports. The world
keeps deciding geometry; the controller uses `Move` and nothing else, so every rule runs on both backends.

## F. Integration

- **State hash.** No new field of state: the new values are parameters. `StandOn` and `Restore` write fields that
  were already hashed. A restored controller hashes as the one that was kept (tested).
- **Determinism.** With every new value at its default, 20,000 seeded steps (40 random motions over the old
  parameters, a one-way platform, a moving platform, launches and teleports) gave the same position, velocity,
  events, ground and hash as the controller of `main` in every step, on the tile backend and on Box2D in metres and
  in pixels. The comparison ran against a copy of the old source and is not kept in the repository.
- **Scenes.** Unchanged: `Physics.Track(controller)`.
- **Backends.** `Stopped` is exact on tiles and derived from the move's shortfall on Box2D (managed code). The native
  layer is unchanged, version 7.

## G. Trade-offs

- `CharacterMotion` grows from 24 to 34 values. The manual groups them by what the player feels; none is needed for
  a first character.
- `LandingReach` costs one more `Move` in a step that ends in the air while falling, and `StandOn` ground costs a
  step that finds no ground followed by the game's call.
- `Order` is a setting a developer has to know exists. It is described by its visible effect (what a jump off a
  moving platform takes with it).
- A landing through `StandOn` counts the landing step in `AirTime`.
- `Position` and `Teleport` place without collision, and a body ignores what it overlaps: a place inside a solid is
  a way through it. `Shift` is the collided form; the manual says which to use for a camera clamp.
- `Stopped` on Box2D is a threshold of a few float steps at the body's distance from the origin; the shared tests
  run it at 40,000 units from the origin on both axes and both signs.

## H. Decision

**Extend.** The controller's model (rules over `Move`, intent in, state out) is right and stays; it lacked
vocabulary and three doors.

## I. Validation

- `tests/MeadowRun.Tests`: the controller beside Pip's mover, same input. Before: 66 of 94,285 steps diverged, 52 of
  62 rules identical. Now: 0 of 94,285, 62 of 62, and the tests fail on the first difference. Compared exactly after
  every step: position, velocity, ground, whether a jump can be cut, the coyote, buffer and drop timers, and the
  step's events. The controller runs on its own from the start of each attempt to its end: the stage's two bounces
  are a placement and a `Launch`, and its state is never copied from Pip's. The 94,285 steps hold about 6,500
  different situations (a level, Pip's position, velocity, ground and jump state, and the input): the autopilot
  repeats attempts and waits out the timer on levels 4 and 5. The game was not edited.
- `tests/Shared/Physics/CharacterFeelConformance.cs`: each rule on both backends, with the default as the contrast
  in the same test. Neon Rift's hop is written as the game writes it and compared step by step.
- `tests/MicroEngine.Tests/Physics/TileMotionAgreementTests.cs`: Courtyard Key's measurement repeated (6000 seeded
  steps, a 10 × 10 box at 70 px/s among random tiles). Origin at the centre: 3061 steps differ, worst 0.0002 px.
  Origin at the corner: 1 step differs. The cause of the many is arithmetic: the world keeps the origin and derives
  the corner (`origin - half`) before each sweep and adds the move to the origin, where the helper adds it to the
  corner. The cause of the one is a rule: a move up or left that ends less than 0.001 inside a tile is pushed out
  by `TileMotion` and left as touching by the world.
- `samples/PhysicsPlayground`: the classic room on one motion; its tour was run and its captures looked at.

Not validated: no demo was moved onto the controller (not in this work's surface); Trail Duel has no character of
this kind; Box2D on platforms other than win-x64.
