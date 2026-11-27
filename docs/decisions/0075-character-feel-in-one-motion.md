# 0075 — A character's feel is one motion; what is the game's own goes through three doors

Status: Accepted (2026-11-26). The SDK is in Alpha: public API changes carry no compatibility commitment. Extends
[0068](0068-physics-contract-version-2.md) (character controllers over `Move`). Evidence:
[GAPS-CHARACTER](../evaluations/gaps-character.md). Issue NastMz/MicroEngine#218, group B.

## Context

The four demo games were moved onto the SDK under the rule that nothing a player sees changes. None of them could
take `CharacterController2D`. Meadow Run measured why: its own mover and the controller, with the same input over
its five levels, disagreed in 66 of 94,285 steps with the best mapping the public API allowed, a motion record
chosen by the game in every step. The disagreements were not errors of the controller: they were rules a classic
platform game has and the controller could not be told (a run, the air's own numbers, momentum, a landing a pixel
early, down and jump, a bounce the player shapes), an order (does a jump leave a platform before or after the
platform moves the character), and three things a game does around a character that the controller did not let it
do (keep its state, place it without taking it off the ground, decide its ground).

## Decision

- **The feel is data, in one `CharacterMotion`.** A run (`RunSpeed`, `RunAcceleration`, `CharacterInput.Run`), the
  air's acceleration and drag as their own numbers (`AirAcceleration`, `AirDeceleration`; `AirControl` stays as the
  one-number form), what happens to speed above what the intent asks for (`OverspeedDeceleration` on the ground,
  `AirOverspeedDeceleration` in the air: 0 is a jump that keeps its momentum), a landing reach (`LandingReach`), a
  released jump cut to a share of its speed (`JumpReleaseSpeedScale`), a drop that needs a jump (`DropWithJump`,
  which also takes the buffered jump) and the order of the ground's carry and the jump (`Order`). `RunJumpBonus` is
  measured against the motion's top speed, so the same speed gives the same jump whether run is held or not. A game
  does not swap records to run or to be in the air.
- **Every new value defaults to what the controller did before.** With them at their defaults the controller is
  bit-identical to the previous one on both backends, state hash included.
- **Three doors for what is the game's own**, instead of a parameter per game:
  - `Launch(velocity, cuttable)`: a rise the player shapes like a jump.
  - `StandOn(shape, position)`: ground the game decides with a rule of its own; the controller then treats it as
    ground (carry, jump, coyote time).
  - `Snapshot()` / `Restore(state)` with a public `CharacterState`, and a settable `Position` that changes nothing
    else. `Teleport` keeps meaning "to another place, in the air".
- **Where the position is, is the game's choice.** `Create` takes the box's centre relative to the body's origin
  (`offset`), for both controllers. A game that places its characters by their feet makes the origin the feet; the
  transform a scene's physics writes is then the feet. No anchor is added to the transform sync.
- **A move says what stopped it.** `MoveContact.Stopped` (and `MoveResult.StoppedGoing(direction)`): the surface
  stopped the body in this move, as opposed to being touched where the move ended. Exact on the tile backend; on
  Box2D derived from the shortfall of the move along the contact's normal, in managed code. The native layer does
  not change.
- **Tiles seen by kinds of mover are views of one grid** (`ICollisionGrid.Blocking(flags)`), each added in a layer
  of its own. `AddTiles` keeps taking one grid and one layer.

## Declined, with reasons

- **A landing rule for moving platforms as parameters** (Meadow Run's platforms catch Pip half a pixel before he
  touches and 2.5 pixels after, a pixel in from their ends). It is one game's rule; `StandOn` lets the game keep it.
- **Making the tile world agree with `TileMotion` in every bit.** With the body's origin at the box's corner the two
  agree except in a move toward smaller coordinates that ends less than 0.001 units inside a tile: `TileMotion`
  pushes the box out for that direction only (an asymmetry of its cell ranges), the world has one tolerance for all
  four. The world's rule is the regular one and stays; the helper cannot change without changing the demos. A test
  pins the difference to that case.
- **A hook between input and move.** The two orders that exist in games are an enum; a callback in the middle of a
  step would let a game change the world while the controller holds results of it.

## Consequences

- Meadow Run's differential is now a proof and fails on any step that differs: 94,285 steps over the five levels and
  62 single-rule runs, none diverge, with one motion, the input passed as it is, and two rules written by the game
  after the step (a hidden block stops the head; the platform's catch through `StandOn`).
- `CharacterMotion` has 9 more values and one enum. The manual explains them by what the player feels, and the
  playground's classic room is the example.
- `CharacterInput`, `Create` and `Launch` have one more optional parameter each: source compatible, not binary
  compatible.
- The demos still use their own movers; moving them is a later pass with its own identity check.
