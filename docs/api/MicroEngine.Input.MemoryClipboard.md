# Class MemoryClipboard

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

A clipboard that only remembers its text: the headless platform's, and a stand-in where there is no system clipboard.

```csharp
public sealed class MemoryClipboard : IClipboard
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MemoryClipboard](MicroEngine.Input.MemoryClipboard.md)

#### Implements

[IClipboard](MicroEngine.Input.IClipboard.md)

## Constructors

### <a id="MicroEngine_Input_MemoryClipboard__ctor"></a> MemoryClipboard\(\)

```csharp
public MemoryClipboard()
```

## Properties

### <a id="MicroEngine_Input_MemoryClipboard_Text"></a> Text

The text on this clipboard, or null.

```csharp
public string? Text { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

## Methods

### <a id="MicroEngine_Input_MemoryClipboard_GetText"></a> GetText\(\)

The text on the clipboard; null when it holds no text or cannot be read.

```csharp
public string? GetText()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Input_MemoryClipboard_SetText_System_String_"></a> SetText\(string\)

Puts <code class="paramref">text</code> on the clipboard; returns whether the platform took it.

```csharp
public bool SetText(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
