using System.Numerics;

namespace EcsBench;

// The components of the three component stores: plain structs. Friflo.Engine.ECS asks components to carry its marker
// interface, which costs the other stores nothing; one set of types keeps the three scenes the same.

internal struct Position(Vector2 value) : Friflo.Engine.ECS.IComponent { public Vector2 Value = value; }
/// <summary>The position at the start of the step, for interpolated drawing.</summary>
internal struct Previous(Vector2 value) : Friflo.Engine.ECS.IComponent { public Vector2 Value = value; }
internal struct Velocity(Vector2 value) : Friflo.Engine.ECS.IComponent { public Vector2 Value = value; }
internal struct Life(int steps) : Friflo.Engine.ECS.IComponent { public int Steps = steps; }
internal struct Health(int value) : Friflo.Engine.ECS.IComponent { public int Value = value; }
internal struct Burning(int damage) : Friflo.Engine.ECS.IComponent { public int Damage = damage; }
