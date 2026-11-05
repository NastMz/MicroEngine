# Class RaylibTouchScreen

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

Touches for the raylib platform. raylib's own touch state is a per-frame list of points indexed by position (no
cancel, no reliable identity across frames), so it is not used: a raylib window reports no touches; its controls
still lay out for scripted touches.

```csharp
public sealed class RaylibTouchScreen : ITouchScreen
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[RaylibTouchScreen](MicroEngine.Raylib.RaylibTouchScreen.md)

#### Implements

ITouchScreen

## Constructors

### <a id="MicroEngine_Raylib_RaylibTouchScreen__ctor"></a> RaylibTouchScreen\(\)

A touch screen with no page feeding it (a desktop window): no touches, default insets and density.

```csharp
public RaylibTouchScreen()
```

## Properties

### <a id="MicroEngine_Raylib_RaylibTouchScreen_CanVibrate"></a> CanVibrate

Whether [Vibrate](MicroEngine.Input.ITouchScreen.md#MicroEngine_Input_ITouchScreen_Vibrate_System_Single_System_Int32_) can do anything here.

```csharp
public bool CanVibrate { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Raylib_RaylibTouchScreen_Overlay"></a> Overlay

Drawn over the letterboxed game in screen pixels after every frame (never in captures).

```csharp
public ITouchOverlay? Overlay { get; set; }
```

#### Property Value

 ITouchOverlay?

### <a id="MicroEngine_Raylib_RaylibTouchScreen_PixelsPerDp"></a> PixelsPerDp

Pixels per density-independent pixel (1/160 inch): 1 on a classic desktop display, 2-4 on phones.

```csharp
public float PixelsPerDp { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Raylib_RaylibTouchScreen_SafeArea"></a> SafeArea

The part of [Size](MicroEngine.Input.ITouchScreen.md#MicroEngine_Input_ITouchScreen_Size) no notch, rounded corner or system bar covers.

```csharp
public RectF SafeArea { get; }
```

#### Property Value

 RectF

### <a id="MicroEngine_Raylib_RaylibTouchScreen_Size"></a> Size

Drawable size in pixels.

```csharp
public Vector2 Size { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Raylib_RaylibTouchScreen_TouchFirst"></a> TouchFirst

Whether touch is the main way to play here (a phone, a tablet, a touch-only browser).

```csharp
public bool TouchFirst { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Raylib_RaylibTouchScreen_ReadTouches_System_Collections_Generic_List_MicroEngine_Input_TouchEvent__"></a> ReadTouches\(List<TouchEvent\>\)

Moves every touch event since the previous call into <code class="paramref">events</code>, oldest first.

```csharp
public void ReadTouches(List<TouchEvent> events)
```

#### Parameters

`events` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<TouchEvent\>

### <a id="MicroEngine_Raylib_RaylibTouchScreen_Vibrate_System_Single_System_Int32_"></a> Vibrate\(float, int\)

A short vibration (strength 0..1). Returns false when the device cannot vibrate.

```csharp
public bool Vibrate(float strength, int milliseconds)
```

#### Parameters

`strength` [float](https://learn.microsoft.com/dotnet/api/system.single)

`milliseconds` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
