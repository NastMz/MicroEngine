# Enum CharacterEvents

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

What happened to a [CharacterController2D](MicroEngine.Physics.CharacterController2D.md) during its latest step.

```csharp
[Flags]
public enum CharacterEvents
```

## Fields

`CornerCorrected = 128` 

Its rise was about to clip the corner of a ceiling and it was moved sideways past it.



`DroppedThrough = 32` 

It started falling through the one-way platform it stood on.



`HitCeiling = 8` 

Its rise was stopped by a ceiling.



`HitWall = 16` 

It began touching a wall.



`Jumped = 4` 

A jump started (from the ground, in coyote time, in the air or off a wall).



`Landed = 1` 

It was in the air and now stands on ground.



`LeftGround = 2` 

It stood on ground and no longer does (a jump, a drop, a ledge, a launch).



`None = 0` 

Nothing.



`Skidded = 64` 

It skidded in this step: on the ground, braking against its motion before it turns.

</div>
