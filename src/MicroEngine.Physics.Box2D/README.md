# MicroEngine.Physics.Box2D

The Box2D v3 backend of MicroEngine's 2D physics contract (`IPhysicsWorld2D`): `Box2DPhysicsWorld2D`, native Box2D
3.2.0 behind the engine's own flat C layer. Besides the common contract it declares dynamic bodies, rotation, circles,
capsules, convex polygons, continuous collision, friction and restitution, mass properties and one-way platforms; an
undeclared request throws `PhysicsCapabilityException`.

```csharp
using var physics = new Box2DPhysicsWorld2D(new Box2DWorldOptions { UnitsPerMeter = 32, YDown = true });
```

Optional and not published yet: it is not one of the SDK packages. The package carries the native library for
win-x64, linux-x64, osx-arm64, osx-x64, android-arm64, android-x64 and ios-arm64 (`runtimes/<rid>/native`) and the
static libraries browser builds link (`buildTransitive` adds them). Box2D is MIT-licensed (`LICENSE-box2d.txt`).
