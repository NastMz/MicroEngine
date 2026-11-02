# Interface ITouchScreen

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.dll  
Guide: [Input](../manual/input.md)  

A screen that may be touched, reported by a backend ([Touch](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_Touch)): its drawable size, the
area free of notches, rounded corners and system bars, its density, the touches since the last frame, and a layer
drawn over the game for on-screen controls. Coordinates are drawable pixels with the origin at the top left.
SDL reports phones and touch screens, the browser host reports Pointer Events, the headless platform lets tests
touch it ([HeadlessTouchScreen](MicroEngine.Core.HeadlessTouchScreen.md)).

```csharp
public interface ITouchScreen
```

## Properties

### <a id="MicroEngine_Input_ITouchScreen_CanVibrate"></a> CanVibrate

Whether [Vibrate](MicroEngine.Input.ITouchScreen.md#MicroEngine_Input_ITouchScreen_Vibrate_System_Single_System_Int32_) can do anything here.

```csharp
bool CanVibrate { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_ITouchScreen_Overlay"></a> Overlay

Drawn over the letterboxed game in screen pixels after every frame (never in captures).

```csharp
ITouchOverlay? Overlay { get; set; }
```

#### Property Value

 [ITouchOverlay](MicroEngine.Input.ITouchOverlay.md)?

### <a id="MicroEngine_Input_ITouchScreen_PixelsPerDp"></a> PixelsPerDp

Pixels per density-independent pixel (1/160 inch): 1 on a classic desktop display, 2-4 on phones.

```csharp
float PixelsPerDp { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Input_ITouchScreen_SafeArea"></a> SafeArea

The part of [Size](MicroEngine.Input.ITouchScreen.md#MicroEngine_Input_ITouchScreen_Size) no notch, rounded corner or system bar covers.

```csharp
RectF SafeArea { get; }
```

#### Property Value

 RectF

### <a id="MicroEngine_Input_ITouchScreen_Size"></a> Size

Drawable size in pixels.

```csharp
Vector2 Size { get; }
```

#### Property Value

 [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

### <a id="MicroEngine_Input_ITouchScreen_TouchFirst"></a> TouchFirst

Whether touch is the main way to play here (a phone, a tablet, a touch-only browser).

```csharp
bool TouchFirst { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Input_ITouchScreen_ReadTouches_System_Collections_Generic_List_MicroEngine_Input_TouchEvent__"></a> ReadTouches\(List<TouchEvent\>\)

Moves every touch event since the previous call into <code class="paramref">events</code>, oldest first.

```csharp
void ReadTouches(List<TouchEvent> events)
```

#### Parameters

`events` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[TouchEvent](MicroEngine.Input.TouchEvent.md)\>

### <a id="MicroEngine_Input_ITouchScreen_Vibrate_System_Single_System_Int32_"></a> Vibrate\(float, int\)

A short vibration (strength 0..1). Returns false when the device cannot vibrate.

```csharp
bool Vibrate(float strength, int milliseconds)
```

#### Parameters

`strength` [float](https://learn.microsoft.com/dotnet/api/system.single)

`milliseconds` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
