# Interface IClipboard

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The system clipboard's text: a platform capability (<code>Game.TryGetCapability</code>). Text fields copy, cut and paste
through it (<code>MicroEngine.UI.TextField</code>); a game may use it for its own "copy the seed" or "paste a code" buttons.

```csharp
public interface IClipboard
```

## Remarks

On desktops it is the system clipboard. In a browser, reading gives the text of the page's latest paste (the
player pressed Ctrl+V or Command+V, or chose Paste in the browser's menu), because a page cannot read the
clipboard without asking; writing puts the text on the system clipboard when the browser allows it, which it does
right after a key press or a click.

## Methods

### <a id="MicroEngine_Input_IClipboard_GetText"></a> GetText\(\)

The text on the clipboard; null when it holds no text or cannot be read.

```csharp
string? GetText()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_Input_IClipboard_SetText_System_String_"></a> SetText\(string\)

Puts <code class="paramref">text</code> on the clipboard; returns whether the platform took it.

```csharp
bool SetText(string text)
```

#### Parameters

`text` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
