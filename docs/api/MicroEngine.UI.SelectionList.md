# Class SelectionList

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.dll  
Guide: [UI widgets](../manual/ui.md)  

A vertical list of options navigated with up/down and chosen with confirm.

```csharp
public sealed class SelectionList
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SelectionList](MicroEngine.UI.SelectionList.md)

## Constructors

### <a id="MicroEngine_UI_SelectionList__ctor_System_String___"></a> SelectionList\(params string\[\]\)

Creates a list with the first option highlighted.

```csharp
public SelectionList(params string[] options)
```

#### Parameters

`options` [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]

#### Exceptions

 [ArgumentException](https://learn.microsoft.com/dotnet/api/system.argumentexception)

<code class="paramref">options</code> is empty.

## Properties

### <a id="MicroEngine_UI_SelectionList_Index"></a> Index

The highlighted option. Setting it is not range-checked; keep it within [Options](MicroEngine.UI.SelectionList.md#MicroEngine_UI_SelectionList_Options).

```csharp
public int Index { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_SelectionList_Options"></a> Options

The option labels, top to bottom.

```csharp
public IReadOnlyList<string> Options { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_UI_SelectionList_Wrap"></a> Wrap

Whether moving past either end jumps to the other (true by default).

```csharp
public bool Wrap { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_UI_SelectionList_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_BitmapFont_System_Numerics_Vector2_MicroEngine_UI_UiTheme_System_Single_System_Int32_"></a> Draw\(IRenderer, BitmapFont, Vector2, UiTheme, float, int\)

Draws the options with a cursor beside the highlighted one.

```csharp
public void Draw(IRenderer r, BitmapFont font, Vector2 position, UiTheme theme, float time, int lineHeight = 12)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

The renderer.

`font` [BitmapFont](MicroEngine.Graphics.BitmapFont.md)

The font.

`position` [Vector2](https://learn.microsoft.com/dotnet/api/system.numerics.vector2)

Top-left of the list, in virtual pixels.

`theme` [UiTheme](MicroEngine.UI.UiTheme.md)

Colours.

`time` [float](https://learn.microsoft.com/dotnet/api/system.single)

Seconds, for the cursor's bobbing (for example [Age](MicroEngine.Scenes.Scene.md#MicroEngine_Scenes_Scene_Age)).

`lineHeight` [int](https://learn.microsoft.com/dotnet/api/system.int32)

Distance between options in pixels.

### <a id="MicroEngine_UI_SelectionList_IsEnabled_System_Int32_"></a> IsEnabled\(int\)

Whether the option at <code class="paramref">index</code> can be chosen.

```csharp
public bool IsEnabled(int index)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_SelectionList_SetEnabled_System_Int32_System_Boolean_"></a> SetEnabled\(int, bool\)

Enables or disables an option. A disabled option can be highlighted but not chosen, and is drawn in [Muted](MicroEngine.UI.UiTheme.md#MicroEngine_UI_UiTheme_Muted).

```csharp
public void SetEnabled(int index, bool enabled)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_SelectionList_SetOptions_System_Collections_Generic_IEnumerable_System_String__"></a> SetOptions\(IEnumerable<string\>\)

Replaces the labels, keeping [Index](MicroEngine.UI.SelectionList.md#MicroEngine_UI_SelectionList_Index) within range. Disabled indices stay disabled.

```csharp
public void SetOptions(IEnumerable<string> options)
```

#### Parameters

`options` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[string](https://learn.microsoft.com/dotnet/api/system.string)\>

### <a id="MicroEngine_UI_SelectionList_Update_MicroEngine_Input_InputMap_System_String_System_String_System_String_"></a> Update\(InputMap, string, string, string\)

Returns the chosen index when confirmed, otherwise null.

```csharp
public int? Update(InputMap input, string up = "up", string down = "down", string confirm = "confirm")
```

#### Parameters

`input` [InputMap](MicroEngine.Input.InputMap.md)

`up` [string](https://learn.microsoft.com/dotnet/api/system.string)

`down` [string](https://learn.microsoft.com/dotnet/api/system.string)

`confirm` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_UI_SelectionList_Moved"></a> Moved

Raised when the highlighted option changes (for a cursor sound).

```csharp
public event Action? Moved
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>
