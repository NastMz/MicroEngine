# Class TextInput

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The text channel, separate from actions: while it is on, characters typed (and the text an input method is
composing) arrive here instead of as keys to remember. Turn it on for name entry, chat or a console; on phones it
shows the on-screen keyboard. A layer above gameplay (a text field) may take text events itself.

```csharp
public sealed class TextInput
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TextInput](MicroEngine.Input.TextInput.md)

## Constructors

### <a id="MicroEngine_Input_TextInput__ctor"></a> TextInput\(\)

```csharp
public TextInput()
```

## Properties

### <a id="MicroEngine_Input_TextInput_Active"></a> Active

Whether text input is on.

```csharp
public bool Active { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Input_TextInput_Composition"></a> Composition

The text an input method is composing and has not committed yet (empty when none).

```csharp
public string Composition { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_Input_TextInput_Typed"></a> Typed

The characters typed since the previous update, in order (empty while off). Backspace and Enter are keys, never text.

```csharp
public string Typed { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_Input_TextInput_Start_MicroEngine_RectF_"></a> Start\(RectF\)

Turns text input on. <code class="paramref">caret</code> is where the text being edited is, in virtual canvas pixels, so an
input method can show its candidates next to it.

```csharp
public void Start(RectF caret = default)
```

#### Parameters

`caret` [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_Input_TextInput_Stop"></a> Stop\(\)

Turns text input off and drops what was being composed.

```csharp
public void Stop()
```

</div>
