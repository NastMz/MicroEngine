# Class Typewriter

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.dll  
Guide: [UI widgets](../manual/ui.md)  

Reveals text character by character. Confirm skips to the end; a second confirm advances.

```csharp
public sealed class Typewriter
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Typewriter](MicroEngine.UI.Typewriter.md)

## Constructors

### <a id="MicroEngine_UI_Typewriter__ctor"></a> Typewriter\(\)

```csharp
public Typewriter()
```

## Properties

### <a id="MicroEngine_UI_Typewriter_CharactersPerSecond"></a> CharactersPerSecond

Reveal speed (45 by default).

```csharp
public float CharactersPerSecond { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_Typewriter_Complete"></a> Complete

Whether the whole text is revealed.

```csharp
public bool Complete { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_Typewriter_Text"></a> Text

The full text being revealed.

```csharp
public string Text { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_Typewriter_Visible"></a> Visible

The revealed part of the text.

```csharp
public string Visible { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_Typewriter_VisibleCount"></a> VisibleCount

Number of characters revealed so far.

```csharp
public int VisibleCount { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_UI_Typewriter_Finish"></a> Finish\(\)

Reveals the rest of the text at once, without raising [Revealed](MicroEngine.UI.Typewriter.md#MicroEngine_UI_Typewriter_Revealed).

```csharp
public void Finish()
```

### <a id="MicroEngine_UI_Typewriter_Start_System_String_"></a> Start\(string\)

Starts revealing <code class="paramref">text</code> from the beginning; null counts as empty.

```csharp
public void Start(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_Typewriter_Update_System_Single_"></a> Update\(float\)

Reveals more characters for <code class="paramref">dt</code> seconds and raises [Revealed](MicroEngine.UI.Typewriter.md#MicroEngine_UI_Typewriter_Revealed) for each new visible one.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_Typewriter_Revealed"></a> Revealed

Raised for each newly revealed non-space character (for blip sounds).

```csharp
public event Action<char>? Revealed
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>?
