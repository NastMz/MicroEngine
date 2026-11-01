# Class BrowserTouch

Namespace: [MicroEngine.Raylib](MicroEngine.Raylib.md)  
Assembly: MicroEngine.Raylib.dll  
Guide: [Backends and platforms](../manual/backends.md)  

The browser page's way into the raylib platform's touch screen ([RaylibTouchScreen](MicroEngine.Raylib.RaylibTouchScreen.md)): the page listens to
Pointer Events on the canvas and forwards touch and pen pointers here, in canvas pixels, with the page's safe-area
insets (CSS <code>env(safe-area-inset-*)</code>) and density. The browser host (src/MicroEngine.Cli/Hosts/web) calls these from its JS exports.

```csharp
public static class BrowserTouch
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BrowserTouch](MicroEngine.Raylib.BrowserTouch.md)

## Properties

### <a id="MicroEngine_Raylib_BrowserTouch_Vibrator"></a> Vibrator

How the page vibrates (navigator.vibrate); null where it cannot.

```csharp
public static Func<int, bool>? Vibrator { get; set; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

## Methods

### <a id="MicroEngine_Raylib_BrowserTouch_Pointer_System_Int64_System_Int32_System_Single_System_Single_"></a> Pointer\(long, int, float, float\)

Phase: 0 began, 1 moved, 2 ended, 3 cancelled. Position in canvas (render) pixels.

```csharp
public static void Pointer(long id, int phase, float x, float y)
```

#### Parameters

`id` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`phase` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`x` [float](https://learn.microsoft.com/dotnet/api/system.single)

`y` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Raylib_BrowserTouch_Screen_System_Single_System_Single_System_Single_System_Single_System_Single_System_Boolean_"></a> Screen\(float, float, float, float, float, bool\)

The screen facts the page knows: safe-area insets in canvas pixels, canvas pixels per CSS pixel, and whether touch is the main input.

```csharp
public static void Screen(float left, float top, float right, float bottom, float pixelsPerDp, bool touchFirst)
```

#### Parameters

`left` [float](https://learn.microsoft.com/dotnet/api/system.single)

`top` [float](https://learn.microsoft.com/dotnet/api/system.single)

`right` [float](https://learn.microsoft.com/dotnet/api/system.single)

`bottom` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pixelsPerDp` [float](https://learn.microsoft.com/dotnet/api/system.single)

`touchFirst` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
