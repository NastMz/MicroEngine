# Struct Transform

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Scenes](MicroEngine.Scenes.md)  
Assembly: MicroEngine.Scene.dll  
Guide: [Scenes](../manual/scenes.md)  

Position, rotation and scale relative to the parent (or to the world for a root entity). Stored in 3D; 2D games use
the <code>2D</code> helpers, which read and write X/Y and the rotation about Z. <code>default</code> is degenerate (zero scale and
a zero quaternion): entities start at [Identity](MicroEngine.Scenes.Transform.md#MicroEngine_Scenes_Transform_Identity).

```csharp
public struct Transform
```

## Fields

### <a id="MicroEngine_Scenes_Transform_Position"></a> Position

Translation.

```csharp
public Vector3 Position
```

#### Field Value

 [Vector3](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

### <a id="MicroEngine_Scenes_Transform_Rotation"></a> Rotation

Rotation (unit quaternion).

```csharp
public Quaternion Rotation
```

#### Field Value

 [Quaternion](https://learn.microsoft.com/dotnet/api/system.numerics.quaternion)

### <a id="MicroEngine_Scenes_Transform_Scale"></a> Scale

Scale per axis.

```csharp
public Vector3 Scale
```

#### Field Value

 [Vector3](https://learn.microsoft.com/dotnet/api/system.numerics.vector3)

## Properties

### <a id="MicroEngine_Scenes_Transform_Identity"></a> Identity

No translation, no rotation, unit scale.

```csharp
public static Transform Identity { get; }
```

#### Property Value

 [Transform](MicroEngine.Scenes.Transform.md)

### <a id="MicroEngine_Scenes_Transform_Position2D"></a> Position2D

X and Y of [Position](MicroEngine.Scenes.Transform.md#MicroEngine_Scenes_Transform_Position); writing keeps Z. Exact: a written value reads back bit for bit.

```csharp
public Vector2 Position2D { readonly get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Scenes_Transform_Rotation2D"></a> Rotation2D

The rotation about Z in radians. Writing replaces [Rotation](MicroEngine.Scenes.Transform.md#MicroEngine_Scenes_Transform_Rotation); reading converts back from the
quaternion, so a written angle reads back within a few ulps, not bit for bit (keep the angle in game state when
the exact value matters).

```csharp
public float Rotation2D { readonly get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Scenes_Transform_Scale2D"></a> Scale2D

X and Y of [Scale](MicroEngine.Scenes.Transform.md#MicroEngine_Scenes_Transform_Scale); writing keeps Z.

```csharp
public Vector2 Scale2D { readonly get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Methods

### <a id="MicroEngine_Scenes_Transform_Interpolate_MicroEngine_Scenes_Transform__MicroEngine_Scenes_Transform__System_Single_"></a> Interpolate\(in Transform, in Transform, float\)

The transform <code class="paramref">t</code> of the way from <code class="paramref">from</code> to <code class="paramref">to</code>: positions and
scales lerp, rotations slerp. Returns <code class="paramref">to</code> exactly when <code class="paramref">t</code> is 1 or more or the
two are equal, so a fixed-step run draws exactly the simulated state.

```csharp
public static Transform Interpolate(in Transform from, in Transform to, float t)
```

#### Parameters

`from` [Transform](MicroEngine.Scenes.Transform.md)

`to` [Transform](MicroEngine.Scenes.Transform.md)

`t` [float](https://learn.microsoft.com/dotnet/api/system.single)

#### Returns

 [Transform](MicroEngine.Scenes.Transform.md)

### <a id="MicroEngine_Scenes_Transform_ToMatrix"></a> ToMatrix\(\)

The matrix that maps this transform's space to its parent's (scale, then rotate, then translate).

```csharp
public readonly Matrix4x4 ToMatrix()
```

#### Returns

 [Matrix4x4](https://learn.microsoft.com/dotnet/api/system.numerics.matrix4x4)

</div>
