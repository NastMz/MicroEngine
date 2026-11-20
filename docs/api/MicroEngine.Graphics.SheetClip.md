# Class SheetClip

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Graphics](MicroEngine.Graphics.md)  
Assembly: MicroEngine.Graphics2D.dll  
Guide: [Graphics](../manual/rendering.md)  

A named animation of a sprite sheet: frames by name, how long each shows, and how they play.

```csharp
public sealed record SheetClip : IEquatable<SheetClip>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SheetClip](MicroEngine.Graphics.SheetClip.md)

#### Implements

[IEquatable<SheetClip\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Graphics_SheetClip__ctor_System_String_System_Collections_Generic_IReadOnlyList_System_String__System_Collections_Generic_IReadOnlyList_System_Single__System_Single_MicroEngine_Graphics_AnimationDirection_System_Boolean_"></a> SheetClip\(string, IReadOnlyList<string\>, IReadOnlyList<float\>?, float, AnimationDirection, bool\)

A named animation of a sprite sheet: frames by name, how long each shows, and how they play.

```csharp
public SheetClip(string Name, IReadOnlyList<string> Frames, IReadOnlyList<float>? Durations, float FramesPerSecond, AnimationDirection Direction = AnimationDirection.Forward, bool Loop = true)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

The clip's name ([Clip](MicroEngine.Graphics.SpriteAtlas.md#MicroEngine_Graphics_SpriteAtlas_Clip_System_String_System_Nullable_System_Boolean__)).

`Frames` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

The frames' names, in forward order.

`Durations` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>?

Seconds each frame shows, one per frame; null for a fixed rate (<code class="paramref">FramesPerSecond</code>).

`FramesPerSecond` [float](https://learn.microsoft.com/dotnet/api/system.single)

The rate of a clip without <code class="paramref">Durations</code>.

`Direction` [AnimationDirection](MicroEngine.Graphics.AnimationDirection.md)

The order the frames play in.

`Loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

Whether it starts over after its last frame.

## Properties

### <a id="MicroEngine_Graphics_SheetClip_Direction"></a> Direction

The order the frames play in.

```csharp
public AnimationDirection Direction { get; init; }
```

#### Property Value

 [AnimationDirection](MicroEngine.Graphics.AnimationDirection.md)

### <a id="MicroEngine_Graphics_SheetClip_Durations"></a> Durations

Seconds each frame shows, one per frame; null for a fixed rate (<code class="paramref">FramesPerSecond</code>).

```csharp
public IReadOnlyList<float>? Durations { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>?

### <a id="MicroEngine_Graphics_SheetClip_Frames"></a> Frames

The frames' names, in forward order.

```csharp
public IReadOnlyList<string> Frames { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_Graphics_SheetClip_FramesPerSecond"></a> FramesPerSecond

The rate of a clip without <code class="paramref">Durations</code>.

```csharp
public float FramesPerSecond { get; init; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Graphics_SheetClip_Loop"></a> Loop

Whether it starts over after its last frame.

```csharp
public bool Loop { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SheetClip_Name"></a> Name

The clip's name ([Clip](MicroEngine.Graphics.SpriteAtlas.md#MicroEngine_Graphics_SpriteAtlas_Clip_System_String_System_Nullable_System_Boolean__)).

```csharp
public string Name { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Graphics_SheetClip__Clone__"></a> <Clone\>$\(\)

```csharp
public SheetClip <Clone>$()
```

#### Returns

 [SheetClip](MicroEngine.Graphics.SheetClip.md)

### <a id="MicroEngine_Graphics_SheetClip_Deconstruct_System_String__System_Collections_Generic_IReadOnlyList_System_String___System_Collections_Generic_IReadOnlyList_System_Single___System_Single__MicroEngine_Graphics_AnimationDirection__System_Boolean__"></a> Deconstruct\(out string, out IReadOnlyList<string\>, out IReadOnlyList<float\>?, out float, out AnimationDirection, out bool\)

```csharp
public void Deconstruct(out string Name, out IReadOnlyList<string> Frames, out IReadOnlyList<float>? Durations, out float FramesPerSecond, out AnimationDirection Direction, out bool Loop)
```

#### Parameters

`Name` [string](https://learn.microsoft.com/dotnet/api/system.string)

`Frames` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

`Durations` [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>?

`FramesPerSecond` [float](https://learn.microsoft.com/dotnet/api/system.single)

`Direction` [AnimationDirection](MicroEngine.Graphics.AnimationDirection.md)

`Loop` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SheetClip_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SheetClip_Equals_MicroEngine_Graphics_SheetClip_"></a> Equals\(SheetClip?\)

```csharp
public bool Equals(SheetClip? other)
```

#### Parameters

`other` [SheetClip](MicroEngine.Graphics.SheetClip.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SheetClip_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Graphics_SheetClip_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Graphics_SheetClip_op_Equality_MicroEngine_Graphics_SheetClip_MicroEngine_Graphics_SheetClip_"></a> operator ==\(SheetClip?, SheetClip?\)

```csharp
public static bool operator ==(SheetClip? left, SheetClip? right)
```

#### Parameters

`left` [SheetClip](MicroEngine.Graphics.SheetClip.md)?

`right` [SheetClip](MicroEngine.Graphics.SheetClip.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Graphics_SheetClip_op_Inequality_MicroEngine_Graphics_SheetClip_MicroEngine_Graphics_SheetClip_"></a> operator \!=\(SheetClip?, SheetClip?\)

```csharp
public static bool operator !=(SheetClip? left, SheetClip? right)
```

#### Parameters

`left` [SheetClip](MicroEngine.Graphics.SheetClip.md)?

`right` [SheetClip](MicroEngine.Graphics.SheetClip.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
