# Class TextReveal

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

Text shown a character at a time: the typewriter of a dialogue box, a caption or a sign. It counts characters as a
reader does (a letter with its accents and marks, an emoji made of several code points or a line break is one), so
text in any script is never cut in the middle of one, and it reveals them in reading order, which a right-to-left
line shows growing from its right edge. It holds no look: a [Label](MicroEngine.UI.Label.md) reveals its text with one
([Reveal](MicroEngine.UI.Label.md#MicroEngine_UI_Label_Reveal)), and a box a game draws itself reads [Visible](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_Visible) or
[VisibleLines](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_VisibleLines). Advance it with the clock of whatever shows it (a scene's <code>dt</code>); it allocates
nothing while it runs.

```csharp
public sealed class TextReveal : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[TextReveal](MicroEngine.UI.TextReveal.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Examples

```csharp
var reveal = new TextReveal { CharactersPerSecond = 45 };
reveal.CharacterRevealed += _ => Audio.TryPlay("blip", 0.3f);
reveal.Start("The gems only grow at night.");
// every update
reveal.Update(dt);
if (Input.Pressed("confirm"))
{
    if (!reveal.Complete) reveal.Finish();      // the first press shows the rest
    else NextPage();
}
```

## Constructors

### <a id="MicroEngine_UI_TextReveal__ctor"></a> TextReveal\(\)

```csharp
public TextReveal()
```

## Properties

### <a id="MicroEngine_UI_TextReveal_CharactersPerSecond"></a> CharactersPerSecond

How many characters show per second (45 by default). A value that is not a finite number above zero (zero,
negative, NaN, infinite) is ignored while it lasts: the reveal stays where it is, and nothing is owed when a
valid rate returns.

```csharp
public float CharactersPerSecond { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_TextReveal_Complete"></a> Complete

Whether the whole text shows.

```csharp
public bool Complete { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_TextReveal_Count"></a> Count

How many characters the text has, as a reader counts them (spaces and line breaks included).

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_TextReveal_Lines"></a> Lines

The lines of [Text](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_Text) (separated by <code>\n</code>), whole.

```csharp
public IReadOnlyList<string> Lines { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_UI_TextReveal_PauseAfter"></a> PauseAfter

Seconds the reveal waits after showing a character, given its first code point; null (the default) or zero
waits for none. A pause at the end of a sentence is <code>rune =&gt; rune.Value is '.' or '!' or '?' ? 0.3f : 0</code>.
Nothing waits after the last character of the text.

```csharp
public Func<Rune, float>? PauseAfter { get; set; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[Rune](https://learn.microsoft.com/dotnet/api/system.text.rune), [float](https://learn.microsoft.com/dotnet/api/system.single)\>?

### <a id="MicroEngine_UI_TextReveal_Revealed"></a> Revealed

How many of them show.

```csharp
public int Revealed { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_TextReveal_Text"></a> Text

The whole text being revealed.

```csharp
public string Text { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_TextReveal_Visible"></a> Visible

The part of [Text](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_Text) that shows, without a copy.

```csharp
public ReadOnlySpan<char> Visible { get; }
```

#### Property Value

 [ReadOnlySpan](https://learn.microsoft.com/dotnet/api/system.readonlyspan\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>

### <a id="MicroEngine_UI_TextReveal_VisibleLength"></a> VisibleLength

The length of the part of [Text](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_Text) that shows, in <code>char</code>s: always at the end of a whole
character. A [Label](MicroEngine.UI.Label.md) takes it as its [VisibleCharacters](MicroEngine.UI.Label.md#MicroEngine_UI_Label_VisibleCharacters).

```csharp
public int VisibleLength { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_TextReveal_VisibleLines"></a> VisibleLines

What shows of each of [Lines](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_Lines), for a box that draws its lines itself: a line that shows whole is
that line, one not reached yet is empty, and only the line being revealed is a new string, made when another
character of it shows. The list is the same one until [Start](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_Start_System_String_System_Int32_); its entries change as the text shows.

```csharp
public IReadOnlyList<string> VisibleLines { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="MicroEngine_UI_TextReveal_Finish"></a> Finish\(\)

Shows the rest of the text at once, silently, and ends a pause.

```csharp
public void Finish()
```

### <a id="MicroEngine_UI_TextReveal_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the text, how much of it shows and the pause in course to a state hash: what the next confirm does depends on them.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_UI_TextReveal_Start_System_String_System_Int32_"></a> Start\(string, int\)

Starts revealing <code class="paramref">text</code> (null counts as empty) with its first <code class="paramref">revealed</code>
characters already showing: zero from the beginning, or what was showing when the same text is laid out again.

```csharp
public void Start(string text, int revealed = 0)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

`revealed` [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_TextReveal_Update_System_Single_"></a> Update\(float\)

Shows more of the text for <code class="paramref">dt</code> seconds, raising [CharacterRevealed](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_CharacterRevealed) for each new
character. A step that is not a finite number above zero is ignored, like such a rate
([CharactersPerSecond](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_CharactersPerSecond)): [Revealed](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_Revealed) is always between zero and [Count](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_Count).

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_TextReveal_CharacterRevealed"></a> CharacterRevealed

Raised for each character as it shows, with its first code point, unless it is a space or a line break: play
the typing sound from it. [Finish](MicroEngine.UI.TextReveal.md#MicroEngine_UI_TextReveal_Finish) raises none.

```csharp
public event Action<Rune>? CharacterRevealed
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[Rune](https://learn.microsoft.com/dotnet/api/system.text.rune)\>?

</div>
