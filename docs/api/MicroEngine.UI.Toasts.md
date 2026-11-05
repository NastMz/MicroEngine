# Class Toasts

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

Queued banners that slide in at the top of the screen (or the bottom), one at a time: unlocked achievements, saves,
notifications. Purely visual: they never read input, so play continues underneath. Call [Update](MicroEngine.UI.Toasts.md#MicroEngine_UI_Toasts_Update_System_Single_) every
update and [Draw](MicroEngine.UI.Toasts.md#MicroEngine_UI_Toasts_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IFont_) after the scenes.

```csharp
public sealed class Toasts
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Toasts](MicroEngine.UI.Toasts.md)

## Constructors

### <a id="MicroEngine_UI_Toasts__ctor"></a> Toasts\(\)

```csharp
public Toasts()
```

## Fields

### <a id="MicroEngine_UI_Toasts_ShowTime"></a> ShowTime

Seconds each banner stays (3.4), and seconds it takes to slide in and out (0.25).

```csharp
public const float ShowTime = 3.4
```

#### Field Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_Toasts_SlideTime"></a> SlideTime

Seconds each banner stays (3.4), and seconds it takes to slide in and out (0.25).

```csharp
public const float SlideTime = 0.25
```

#### Field Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Properties

### <a id="MicroEngine_UI_Toasts_Bottom"></a> Bottom

Distance from the bottom edge when fully shown, for games whose HUD sits at the top; null (the default) shows banners at the top.

```csharp
public float? Bottom { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_UI_Toasts_Current"></a> Current

The banner on screen, or null.

```csharp
public Toast? Current { get; }
```

#### Property Value

 [Toast](MicroEngine.UI.Toast.md)?

### <a id="MicroEngine_UI_Toasts_Pending"></a> Pending

Banners waiting behind the current one.

```csharp
public int Pending { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_Toasts_Theme"></a> Theme

Colours of the banners.

```csharp
public UiTheme Theme { get; set; }
```

#### Property Value

 [UiTheme](MicroEngine.UI.UiTheme.md)

### <a id="MicroEngine_UI_Toasts_Top"></a> Top

Distance from the top edge when fully shown.

```csharp
public float Top { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_UI_Toasts_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IFont_"></a> Draw\(IRenderer, IFont\)

Draws the current banner centred at the top of <code class="paramref">r</code> (or the bottom, see [Bottom](MicroEngine.UI.Toasts.md#MicroEngine_UI_Toasts_Bottom)).

```csharp
public void Draw(IRenderer r, IFont font)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`font` [IFont](MicroEngine.Graphics.IFont.md)

### <a id="MicroEngine_UI_Toasts_Show_MicroEngine_UI_Toast_"></a> Show\(Toast\)

Queues <code class="paramref">toast</code>; it shows at once when no other is on screen.

```csharp
public void Show(Toast toast)
```

#### Parameters

`toast` [Toast](MicroEngine.UI.Toast.md)

### <a id="MicroEngine_UI_Toasts_Update_System_Single_"></a> Update\(float\)

Advances the current banner; call it every update.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_Toasts_Advanced"></a> Advanced

Raised when a banner leaves the screen and the next one (or none) takes its place.

```csharp
public event Action? Advanced
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>
