# Enum CharacterStepOrder

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics](../manual/physics.md)  

The order of the things a [CharacterController2D](MicroEngine.Physics.CharacterController2D.md) does in one step ([Order](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_Order)).

```csharp
public enum CharacterStepOrder
```

## Fields

`ActThenCarry = 1` 

The jump or the drop is read first: a character that jumps or drops has left the ground before the ground
moves it, so it is not carried in that step (a jump off a moving platform starts from where the character
stood), and a drop's first step is a step in the air (its gravity is the air's, the floaty top of
[ApexGravityScale](MicroEngine.Physics.CharacterMotion.md#MicroEngine_Physics_CharacterMotion_ApexGravityScale) included). A solid that pushes
([PushesBodies](MicroEngine.Physics.BodyDescription.md#MicroEngine_Physics_BodyDescription_PushesBodies)) has carried its riders before the character steps, whatever the order.



`CarryThenAct = 0` 

The ground carries the character first (a platform that moved since its last step), then the jump or the drop
is read: a jump off a moving platform starts from where the platform took the character in this step.

</div>
