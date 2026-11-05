# Class DefaultTouchSkin

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.Touch.dll  
Guide: [Input](../manual/input.md)  

The built-in look, shaped by [TouchStyle](MicroEngine.Input.TouchStyle.md). Idle controls are an outlined shape at the style's opacity;
a pressed one is filled, outlined thicker and drawn slightly smaller (pressed in), so the press reads by shape as
well as colour, even at low opacity. A dark halo keeps outlines legible on light scenes. A d-pad lights the arrows
it presses and marks where the thumb is; a joystick shows its base, its dead zone, the knob at the thumb's
deflection and an arrow on the rim whose opacity follows the intensity. Images replace fills and outlines.

```csharp
public sealed class DefaultTouchSkin : ITouchSkin
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DefaultTouchSkin](MicroEngine.Input.DefaultTouchSkin.md)

#### Implements

[ITouchSkin](MicroEngine.Input.ITouchSkin.md)

## Constructors

### <a id="MicroEngine_Input_DefaultTouchSkin__ctor"></a> DefaultTouchSkin\(\)

```csharp
public DefaultTouchSkin()
```

## Properties

### <a id="MicroEngine_Input_DefaultTouchSkin_Instance"></a> Instance

The shared instance.

```csharp
public static DefaultTouchSkin Instance { get; }
```

#### Property Value

 [DefaultTouchSkin](MicroEngine.Input.DefaultTouchSkin.md)

## Methods

### <a id="MicroEngine_Input_DefaultTouchSkin_Draw_MicroEngine_Input_IOverlayCanvas_MicroEngine_Input_TouchControlView__"></a> Draw\(IOverlayCanvas, in TouchControlView\)

```csharp
public void Draw(IOverlayCanvas canvas, in TouchControlView v)
```

#### Parameters

`canvas` [IOverlayCanvas](MicroEngine.Input.IOverlayCanvas.md)

`v` [TouchControlView](MicroEngine.Input.TouchControlView.md)

</div>
