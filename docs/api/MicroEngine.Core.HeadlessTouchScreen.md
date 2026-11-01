# Class HeadlessTouchScreen

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

A touch screen for tests: fingers go down, move and lift by hand, the size, safe area and density can change (a
rotation, a notch), vibrations are counted, and [DrawOverlay](MicroEngine.Core.HeadlessTouchScreen.md#MicroEngine_Core_HeadlessTouchScreen_DrawOverlay) records what the overlay draws.

```csharp
public sealed class HeadlessTouchScreen : ITouchScreen
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[HeadlessTouchScreen](MicroEngine.Core.HeadlessTouchScreen.md)

#### Implements

[ITouchScreen](MicroEngine.Input.ITouchScreen.md)

## Constructors

### <a id="MicroEngine_Core_HeadlessTouchScreen__ctor_System_Numerics_Vector2_"></a> HeadlessTouchScreen\(Vector2\)

A touch screen for tests: fingers go down, move and lift by hand, the size, safe area and density can change (a
rotation, a notch), vibrations are counted, and [DrawOverlay](MicroEngine.Core.HeadlessTouchScreen.md#MicroEngine_Core_HeadlessTouchScreen_DrawOverlay) records what the overlay draws.

```csharp
public HeadlessTouchScreen(Vector2 size)
```

#### Parameters

`size` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

## Properties

### <a id="MicroEngine_Core_HeadlessTouchScreen_CanVibrate"></a> CanVibrate

Whether [Vibrate](MicroEngine.Input.ITouchScreen.md#MicroEngine_Input_ITouchScreen_Vibrate_System_Single_System_Int32_) can do anything here.

```csharp
public bool CanVibrate { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessTouchScreen_Overlay"></a> Overlay

Drawn over the letterboxed game in screen pixels after every frame (never in captures).

```csharp
public ITouchOverlay? Overlay { get; set; }
```

#### Property Value

 [ITouchOverlay](MicroEngine.Input.ITouchOverlay.md)?

### <a id="MicroEngine_Core_HeadlessTouchScreen_PixelsPerDp"></a> PixelsPerDp

Pixels per density-independent pixel (1/160 inch): 1 on a classic desktop display, 2-4 on phones.

```csharp
public float PixelsPerDp { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_HeadlessTouchScreen_SafeArea"></a> SafeArea

Defaults to the whole screen.

```csharp
public RectF SafeArea { get; set; }
```

#### Property Value

 [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Core_HeadlessTouchScreen_Size"></a> Size

Drawable size in pixels.

```csharp
public Vector2 Size { get; set; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_HeadlessTouchScreen_TouchFirst"></a> TouchFirst

Whether touch is the main way to play here (a phone, a tablet, a touch-only browser).

```csharp
public bool TouchFirst { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_HeadlessTouchScreen_Vibrations"></a> Vibrations

Successful [Vibrate](MicroEngine.Core.HeadlessTouchScreen.md#MicroEngine_Core_HeadlessTouchScreen_Vibrate_System_Single_System_Int32_) calls so far.

```csharp
public int Vibrations { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_HeadlessTouchScreen_Cancel_System_Int64_"></a> Cancel\(long\)

Cancels <code class="paramref">finger</code>, as the system does when the game loses the screen.

```csharp
public void Cancel(long finger)
```

#### Parameters

`finger` [long](https://learn.microsoft.com/dotnet/api/system.int64)

### <a id="MicroEngine_Core_HeadlessTouchScreen_Down_System_Int64_System_Single_System_Single_"></a> Down\(long, float, float\)

Puts <code class="paramref">finger</code> down at (<code class="paramref">x</code>, <code class="paramref">y</code>) in screen pixels; delivered at the next read.

```csharp
public void Down(long finger, float x, float y)
```

#### Parameters

`finger` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`x` [float](https://learn.microsoft.com/dotnet/api/system.single)

`y` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_HeadlessTouchScreen_Down_System_Int64_System_Numerics_Vector2_"></a> Down\(long, Vector2\)

Puts <code class="paramref">finger</code> down at <code class="paramref">at</code> in screen pixels.

```csharp
public void Down(long finger, Vector2 at)
```

#### Parameters

`finger` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`at` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_HeadlessTouchScreen_DrawOverlay"></a> DrawOverlay\(\)

Draws the overlay into a recorder and returns what it drew.

```csharp
public RecordingOverlayCanvas DrawOverlay()
```

#### Returns

 [RecordingOverlayCanvas](MicroEngine.Core.RecordingOverlayCanvas.md)

### <a id="MicroEngine_Core_HeadlessTouchScreen_Move_System_Int64_System_Numerics_Vector2_"></a> Move\(long, Vector2\)

Moves <code class="paramref">finger</code> to <code class="paramref">at</code>.

```csharp
public void Move(long finger, Vector2 at)
```

#### Parameters

`finger` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`at` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_HeadlessTouchScreen_ReadTouches_System_Collections_Generic_List_MicroEngine_Input_TouchEvent__"></a> ReadTouches\(List<TouchEvent\>\)

Moves every touch event since the previous call into <code class="paramref">events</code>, oldest first.

```csharp
public void ReadTouches(List<TouchEvent> events)
```

#### Parameters

`events` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[TouchEvent](MicroEngine.Input.TouchEvent.md)\>

### <a id="MicroEngine_Core_HeadlessTouchScreen_Up_System_Int64_System_Numerics_Vector2_"></a> Up\(long, Vector2\)

Lifts <code class="paramref">finger</code>.

```csharp
public void Up(long finger, Vector2 at = default)
```

#### Parameters

`finger` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`at` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Core_HeadlessTouchScreen_Vibrate_System_Single_System_Int32_"></a> Vibrate\(float, int\)

Counts the vibration in [Vibrations](MicroEngine.Core.HeadlessTouchScreen.md#MicroEngine_Core_HeadlessTouchScreen_Vibrations); false when [CanVibrate](MicroEngine.Core.HeadlessTouchScreen.md#MicroEngine_Core_HeadlessTouchScreen_CanVibrate) is false.

```csharp
public bool Vibrate(float strength, int milliseconds)
```

#### Parameters

`strength` [float](https://learn.microsoft.com/dotnet/api/system.single)

`milliseconds` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
