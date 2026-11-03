# Interface IScreenView

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [The world view and the UI view](../manual/rendering.md#the-world-view-and-the-ui-view)  

The UI view of a platform that draws at the window's resolution: a capability
([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)) of the SDL and raylib platforms and of the headless
platform. The world is drawn into the canvas ([BeginFrame](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_BeginFrame)) and scaled into
[WorldFrame](MicroEngine.Core.IScreenView.md#MicroEngine_Core_IScreenView_WorldFrame); the UI view is drawn in UI units (window pixels divided by [UiScale](MicroEngine.Core.IScreenView.md#MicroEngine_Core_IScreenView_UiScale)) and
composited over it, so its text and shapes have the window's resolution whatever the canvas's.

```csharp
public interface IScreenView
```

## Properties

### <a id="MicroEngine_Core_IScreenView_CanvasSize"></a> CanvasSize

The canvas's current size in pixels (larger than the configured one with an expanding presentation).

```csharp
(int Width, int Height) CanvasSize { get; }
```

#### Property Value

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

### <a id="MicroEngine_Core_IScreenView_ScreenPixels"></a> ScreenPixels

The window's drawable size in pixels.

```csharp
(int Width, int Height) ScreenPixels { get; }
```

#### Property Value

 \([int](https://learn.microsoft.com/dotnet/api/system.int32) [Width](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.width), [int](https://learn.microsoft.com/dotnet/api/system.int32) [Height](https://learn.microsoft.com/dotnet/api/system.valuetuple\-system.int32,system.int32\-.height)\)

### <a id="MicroEngine_Core_IScreenView_UiScale"></a> UiScale

Window pixels per UI unit ([UiScale](MicroEngine.Graphics.Presentation.md#MicroEngine_Graphics_Presentation_UiScale)).

```csharp
float UiScale { get; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Core_IScreenView_WorldFrame"></a> WorldFrame

Where the canvas lands in the window, in window pixels.

```csharp
RectF WorldFrame { get; }
```

#### Property Value

 RectF

## Methods

### <a id="MicroEngine_Core_IScreenView_BeginUi"></a> BeginUi\(\)

Starts this frame's UI view, transparent; called after [BeginFrame](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_BeginFrame), at most once per frame.

```csharp
IRenderer BeginUi()
```

#### Returns

 [IRenderer](MicroEngine.Graphics.IRenderer.md)

</div>
