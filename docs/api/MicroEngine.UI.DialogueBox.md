# Class DialogueBox

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.dll  
Guide: [UI widgets](../manual/ui.md)  

A queue of dialogue pages shown in a panel with a typewriter and a speaker name tag. A page longer than
[Lines](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Lines) wrapped lines continues on the next confirm, so no text is ever cut. With [Prompts](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Prompts)
set, <code>{action}</code> in a page names the control that presses it on the player's device, and follows a device change
while the page is showing. Games call [Update](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Update_System_Single_MicroEngine_Input_InputMap_System_String_) and [Draw](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IFont_MicroEngine_RectF_) from their own scene.

```csharp
public sealed class DialogueBox
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[DialogueBox](MicroEngine.UI.DialogueBox.md)

## Constructors

### <a id="MicroEngine_UI_DialogueBox__ctor"></a> DialogueBox\(\)

```csharp
public DialogueBox()
```

## Properties

### <a id="MicroEngine_UI_DialogueBox_Active"></a> Active

Whether a page is showing.

```csharp
public bool Active { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_DialogueBox_HasMore"></a> HasMore

Whether the current page has more wrapped lines after the ones showing (known once it has been drawn).

```csharp
public bool HasMore { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_DialogueBox_Lines"></a> Lines

Maximum lines drawn at once (3 by default); a page that wraps to more continues on the next confirm.

```csharp
public int Lines { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_DialogueBox_Prompts"></a> Prompts

Resolves <code>{action}</code> in pages to the player's controls (null: pages are shown as written). Games pass [Prompts](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Prompts).

```csharp
public ActionPrompts? Prompts { get; set; }
```

#### Property Value

 [ActionPrompts](MicroEngine.Input.ActionPrompts.md)?

### <a id="MicroEngine_UI_DialogueBox_Speaker"></a> Speaker

The speaker of the current page, drawn as a name tag; null or empty draws none.

```csharp
public string? Speaker { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_UI_DialogueBox_Text"></a> Text

The text of the current page as shown (prompts resolved), or empty when closed.

```csharp
public string Text { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_DialogueBox_Theme"></a> Theme

Colours of the box ([Default](MicroEngine.UI.UiTheme.md#MicroEngine_UI_UiTheme_Default) by default).

```csharp
public UiTheme Theme { get; set; }
```

#### Property Value

 [UiTheme](MicroEngine.UI.UiTheme.md)

### <a id="MicroEngine_UI_DialogueBox_VisibleLines"></a> VisibleLines

The wrapped lines in the box now, in full even while they are being revealed (known once the page has been drawn).

```csharp
public IReadOnlyList<string> VisibleLines { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

## Methods

### <a id="MicroEngine_UI_DialogueBox_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_IFont_MicroEngine_RectF_"></a> Draw\(IRenderer, IFont, RectF\)

Draws the current page in <code class="paramref">area</code> (virtual pixels), wrapped to its width; draws nothing when closed.
Margins grow with the font: the built-in 10-pixel-high pixel font gets the pixel-art layout, a
[ScalableFont](MicroEngine.Graphics.ScalableFont.md) size with 40-pixel lines gets margins four times as wide.

```csharp
public void Draw(IRenderer r, IFont font, RectF area)
```

#### Parameters

`r` IRenderer

`font` [IFont](MicroEngine.Graphics.IFont.md)

`area` RectF

### <a id="MicroEngine_UI_DialogueBox_Show_System_String_System_String___"></a> Show\(string?, params string\[\]\)

Queues pages spoken by <code class="paramref">speaker</code>; the first one shows at once if the box was closed.

```csharp
public void Show(string? speaker, params string[] pages)
```

#### Parameters

`speaker` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`pages` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

### <a id="MicroEngine_UI_DialogueBox_Update_System_Single_MicroEngine_Input_InputMap_System_String_"></a> Update\(float, InputMap, string?\)

Returns true while the box is open and takes input this update. <code class="paramref">confirm</code> advances it; null (the
default) is the map's confirm action ([Names](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Names)). A confirm the box acted on is consumed
([Consume](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Consume_System_String_)), so nothing read after it (the scene under an overlay) acts on the same press.

```csharp
public bool Update(float dt, InputMap input, string? confirm = null)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

`input` InputMap

`confirm` [string](https://learn.microsoft.com/dotnet/api/system.string)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_DialogueBox_Blip"></a> Blip

Raised for each revealed non-space character, for a typing sound.

```csharp
public event Action<char>? Blip
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[char](https://learn.microsoft.com/dotnet/api/system.char)\>?

### <a id="MicroEngine_UI_DialogueBox_Closed"></a> Closed

Raised when the last page is dismissed.

```csharp
public event Action? Closed
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>
