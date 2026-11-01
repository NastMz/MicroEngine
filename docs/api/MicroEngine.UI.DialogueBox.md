# Class DialogueBox

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.dll  
Guide: [UI widgets](../manual/ui.md)  

A queue of dialogue pages shown in a panel with a typewriter and a speaker name tag.
Games call [Update](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Update_System_Single_MicroEngine_Input_InputMap_System_String_) and [Draw](MicroEngine.UI.DialogueBox.md#MicroEngine_UI_DialogueBox_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_BitmapFont_MicroEngine_RectF_) from their own scene.

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

### <a id="MicroEngine_UI_DialogueBox_Lines"></a> Lines

Maximum lines drawn per page (3 by default); wrapped text beyond it is not shown, so keep pages short.

```csharp
public int Lines { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_DialogueBox_Speaker"></a> Speaker

The speaker of the current page, drawn as a name tag; null or empty draws none.

```csharp
public string? Speaker { get; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)?

### <a id="MicroEngine_UI_DialogueBox_Theme"></a> Theme

Colours of the box ([Default](MicroEngine.UI.UiTheme.md#MicroEngine_UI_UiTheme_Default) by default).

```csharp
public UiTheme Theme { get; set; }
```

#### Property Value

 [UiTheme](MicroEngine.UI.UiTheme.md)

## Methods

### <a id="MicroEngine_UI_DialogueBox_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_BitmapFont_MicroEngine_RectF_"></a> Draw\(IRenderer, BitmapFont, RectF\)

Draws the current page in <code class="paramref">area</code> (virtual pixels), wrapped to its width; draws nothing when closed.

```csharp
public void Draw(IRenderer r, BitmapFont font, RectF area)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

`font` [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

`area` [RectF](MicroEngine.RectF.md)

### <a id="MicroEngine_UI_DialogueBox_Show_System_String_System_String___"></a> Show\(string?, params string\[\]\)

Queues pages spoken by <code class="paramref">speaker</code>; the first one shows at once if the box was closed.

```csharp
public void Show(string? speaker, params string[] pages)
```

#### Parameters

`speaker` [string](https://learn.microsoft.com/dotnet/api/system.string)?

`pages` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

### <a id="MicroEngine_UI_DialogueBox_Update_System_Single_MicroEngine_Input_InputMap_System_String_"></a> Update\(float, InputMap, string\)

Returns true while the box consumed input this update.

```csharp
public bool Update(float dt, InputMap input, string confirm = "confirm")
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

`input` [InputMap](MicroEngine.Input.InputMap.md)

`confirm` [string](https://learn.microsoft.com/dotnet/api/system.string)

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
