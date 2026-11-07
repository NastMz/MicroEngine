# Class BrowserPage

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Sdl](MicroEngine.Sdl.md)  
Assembly: MicroEngine.Sdl.dll  
Guide: [Backends and platforms](../manual/backends.md)  

One browser page's way into the SDL platform it hosts. SDL reads the keyboard, the mouse and gamepads from the page
itself; what it cannot know comes from here: the page listens to Pointer Events on the canvas and forwards touch and
pen pointers, in canvas pixels, with the page's safe-area insets (CSS <code>env(safe-area-inset-*)</code>) and density,
its visibility and how it vibrates. The browser host (src/MicroEngine.Cli/Hosts/web) owns one and calls these from
its JavaScript exports; every platform it builds ([BrowserProfile](MicroEngine.Sdl.SdlPlatform.md#MicroEngine_Sdl_SdlPlatform_BrowserProfile_MicroEngine_Sdl_BrowserPage_)) reads this page and no
other, so nothing is shared through the process.

```csharp
public sealed class BrowserPage
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[BrowserPage](MicroEngine.Sdl.BrowserPage.md)

## Constructors

### <a id="MicroEngine_Sdl_BrowserPage__ctor"></a> BrowserPage\(\)

```csharp
public BrowserPage()
```

## Properties

### <a id="MicroEngine_Sdl_BrowserPage_ClipboardWriter"></a> ClipboardWriter

How the page writes text to the system clipboard (navigator.clipboard); null where it cannot.

```csharp
public Func<string, bool>? ClipboardWriter { get; set; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

### <a id="MicroEngine_Sdl_BrowserPage_Hidden"></a> Hidden

Whether the page is hidden (another tab, the phone locked or switched away), set by the page from
<code>visibilitychange</code>; the SDL platform reports it as a suspension ([Suspending](MicroEngine.Platform.IEngineEventSink.md#MicroEngine_Platform_IEngineEventSink_Suspending)).

```csharp
public bool Hidden { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Sdl_BrowserPage_Vibrator"></a> Vibrator

How the page vibrates (navigator.vibrate) for a number of milliseconds; null where it cannot.

```csharp
public Func<int, bool>? Vibrator { get; set; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[int](https://learn.microsoft.com/dotnet/api/system.int32), [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>?

## Methods

### <a id="MicroEngine_Sdl_BrowserPage_Paste_System_String_"></a> Paste\(string\)

The text of a paste on the page (its <code>paste</code> event): what the game's clipboard reads from now on, because a
page cannot read the system clipboard without asking. The paste's key press reaches the game after it.

```csharp
public void Paste(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Sdl_BrowserPage_Pointer_System_Int64_System_Int32_System_Single_System_Single_"></a> Pointer\(long, int, float, float\)

A touch or pen pointer. Phase: 0 began, 1 moved, 2 ended, 3 cancelled. Position in canvas (render) pixels.

```csharp
public void Pointer(long id, int phase, float x, float y)
```

#### Parameters

`id` [long](https://learn.microsoft.com/dotnet/api/system.int64)

`phase` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`x` [float](https://learn.microsoft.com/dotnet/api/system.single)

`y` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Sdl_BrowserPage_Screen_System_Single_System_Single_System_Single_System_Single_System_Single_System_Boolean_"></a> Screen\(float, float, float, float, float, bool\)

The screen facts the page knows: safe-area insets in canvas pixels, canvas pixels per CSS pixel, and whether touch is the main input.

```csharp
public void Screen(float left, float top, float right, float bottom, float pixelsPerDp, bool touchFirst)
```

#### Parameters

`left` [float](https://learn.microsoft.com/dotnet/api/system.single)

`top` [float](https://learn.microsoft.com/dotnet/api/system.single)

`right` [float](https://learn.microsoft.com/dotnet/api/system.single)

`bottom` [float](https://learn.microsoft.com/dotnet/api/system.single)

`pixelsPerDp` [float](https://learn.microsoft.com/dotnet/api/system.single)

`touchFirst` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
