# Enum JointKind

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Physics](MicroEngine.Physics.md)  
Assembly: MicroEngine.Physics.dll  
Guide: [Physics worlds](../manual/world.md#physics-worlds)  

What a joint constrains.

```csharp
public enum JointKind
```

## Fields

`Distance = 1` 

Keeps two points at a distance: a rod, a spring, or a rope with a limit and no spring force. Limit and coordinate are lengths.



`Prismatic = 2` 

A slider: body B moves along an axis fixed in body A and does not turn (pistons, lifts, doors). Limit and coordinate are translations; the motor is a force.



`Revolute = 0` 

A pin: the two bodies turn about a shared point (hinges, pendulums, chains, ragdoll limbs). Limit and coordinate are angles in radians; the motor is a torque.



`Target = 5` 

Pulls a point of one body toward a world point with a spring of bounded force (dragging a body with the pointer).



`Weld = 3` 

Glues two bodies together; with a spring, a soft glue that bends.



`Wheel = 4` 

A wheel: body B slides along an axis fixed in body A (the suspension, with a spring) and spins freely or by a motor torque.

</div>
