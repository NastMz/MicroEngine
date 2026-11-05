# Interface ITextInputControl

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

Turns the system's text input on and off: the on-screen keyboard on phones, input methods (IME) on desktops. A
capability of input backends that deliver [Text](MicroEngine.Input.InputEventKind.md).

```csharp
public interface ITextInputControl
```

## Methods

### <a id="MicroEngine_Input_ITextInputControl_Start_MicroEngine_RectF_"></a> Start\(RectF\)

Turns text input on; <code class="paramref">caret</code> is where the text being edited is, in virtual canvas pixels, so an
input method can place its candidate list next to it.

```csharp
void Start(RectF caret)
```

#### Parameters

`caret` [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Input_ITextInputControl_Stop"></a> Stop\(\)

Turns text input off.

```csharp
void Stop()
```

</div>
