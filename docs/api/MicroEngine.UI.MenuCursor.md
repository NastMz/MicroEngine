# Class MenuCursor

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

The cursor of a list a scene draws itself: a title menu, a bag, a shop, a two-by-two battle menu. It keeps which
option is highlighted, moves it with the direction actions (wrapping or stopping at the ends, in one column or a
grid), chooses with confirm, lets the cursor rest on an option that cannot be chosen, and gives the first row to
show of a list longer than its box. It holds no labels and draws nothing: the scene draws its rows in its own look
and reads [Index](MicroEngine.UI.MenuCursor.md#MicroEngine_UI_MenuCursor_Index). For a menu with layout, the mouse and touch built in, use widgets instead
([Button](MicroEngine.UI.Button.md) in a [Stack](MicroEngine.UI.Stack.md), with a [ControlSkin](MicroEngine.UI.ControlSkin.md) for the look); with a cursor, the
scene maps its own pointer through [MoveTo](MicroEngine.UI.MenuCursor.md#MicroEngine_UI_MenuCursor_MoveTo_System_Int32_).

```csharp
public sealed class MenuCursor : IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[MenuCursor](MicroEngine.UI.MenuCursor.md)

#### Implements

[IStateHashed](MicroEngine.Kernel.IStateHashed.md)

## Examples

```csharp
private readonly MenuCursor _menu = new(3);          // RESUME, OPTIONS, QUIT

public override void Enter() => _menu.Moved += () => Audio.TryPlay("move");

public override void Update(float dt)
{
    if (_menu.Update(Input) is { } chosen) Choose(chosen);
}

public override void Draw(IRenderer r)
{
    for (int i = 0; i < Labels.Length; i++)
        Font.Draw(r, Labels[i], new Vector2(120, 60 + i * 12), i == _menu.Index ? Gold : Cream);
}
```

## Constructors

### <a id="MicroEngine_UI_MenuCursor__ctor_System_Int32_System_Int32_"></a> MenuCursor\(int, int\)

A cursor on the first of <code class="paramref">count</code> options, in rows of <code class="paramref">columns</code> (1: a vertical list).

```csharp
public MenuCursor(int count, int columns = 1)
```

#### Parameters

`count` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`columns` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

<code class="paramref">count</code> is negative, or <code class="paramref">columns</code> is less than 1.

## Properties

### <a id="MicroEngine_UI_MenuCursor_Columns"></a> Columns

How many options a row has (1 by default: a vertical list, moved by up and down). With more, left and right
move within the row and up and down between rows; a cell that does not exist in a last, shorter row is never reached.

```csharp
public int Columns { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is less than 1.

### <a id="MicroEngine_UI_MenuCursor_Count"></a> Count

How many options the list has; the cursor stays inside when it shrinks. Zero is an empty list: nothing moves or is chosen.

```csharp
public int Count { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [ArgumentOutOfRangeException](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception)

The value is negative.

### <a id="MicroEngine_UI_MenuCursor_Index"></a> Index

The highlighted option; a value outside the list is clamped to its first or last. Setting it raises nothing.

```csharp
public int Index { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_MenuCursor_Wrap"></a> Wrap

Whether moving past an end goes on from the other end of the column or row (true by default).

```csharp
public bool Wrap { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_UI_MenuCursor_HashState_MicroEngine_Kernel_StateHasher_"></a> HashState\(StateHasher\)

Adds the highlighted option, the list's shape, the window and the options that cannot be chosen to a state hash.

```csharp
public void HashState(StateHasher hash)
```

#### Parameters

`hash` [StateHasher](MicroEngine.Kernel.StateHasher.md)

### <a id="MicroEngine_UI_MenuCursor_IsEnabled_System_Int32_"></a> IsEnabled\(int\)

Whether the option at <code class="paramref">index</code> can be chosen.

```csharp
public bool IsEnabled(int index)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_MenuCursor_Move_MicroEngine_UI_UiAction_"></a> Move\(UiAction\)

Moves the cursor one option in <code class="paramref">direction</code>; false when it did not move (an end without [Wrap](MicroEngine.UI.MenuCursor.md#MicroEngine_UI_MenuCursor_Wrap), a single option, confirm or cancel).

```csharp
public bool Move(UiAction direction)
```

#### Parameters

`direction` [UiAction](MicroEngine.UI.UiAction.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_MenuCursor_Move_MicroEngine_Input_InputMap_MicroEngine_UI_UiActions_"></a> Move\(InputMap, UiActions?\)

Moves the cursor by the direction actions pressed in this update: left and right first (in a grid), else up and
down; opposite directions pressed together cancel out. Actions left null in <code class="paramref">actions</code> (or all
of them, by default) are the input map's own names. Returns whether it moved.

```csharp
public bool Move(InputMap input, UiActions? actions = null)
```

#### Parameters

`input` [InputMap](MicroEngine.Input.InputMap.md)

`actions` [UiActions](MicroEngine.UI.UiActions.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_MenuCursor_MoveTo_System_Int32_"></a> MoveTo\(int\)

Puts the cursor on <code class="paramref">index</code> as a move of the player's (the row the pointer is over): raises
[Moved](MicroEngine.UI.MenuCursor.md#MicroEngine_UI_MenuCursor_Moved) when it changes. False when it is already there or is not an option.

```csharp
public bool MoveTo(int index)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_MenuCursor_SetEnabled_System_Int32_System_Boolean_"></a> SetEnabled\(int, bool\)

Says whether the option at <code class="paramref">index</code> can be chosen. The cursor still rests on one that cannot:
[Update](MicroEngine.UI.MenuCursor.md#MicroEngine_UI_MenuCursor_Update_MicroEngine_Input_InputMap_MicroEngine_UI_UiActions_) then leaves the confirm press to the scene, which can answer with a refusal sound.

```csharp
public void SetEnabled(int index, bool enabled)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

`enabled` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_MenuCursor_Top_System_Int32_"></a> Top\(int\)

The first row to draw of a list that shows <code class="paramref">visible</code> rows at a time: it moves just enough to
keep the cursor's row in the window, as a scrolling list does. Call it with the same count every frame.

```csharp
public int Top(int visible)
```

#### Parameters

`visible` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_MenuCursor_Update_MicroEngine_Input_InputMap_MicroEngine_UI_UiActions_"></a> Update\(InputMap, UiActions?\)

One update of the menu: moves the cursor ([Move](MicroEngine.UI.MenuCursor.md#MicroEngine_UI_MenuCursor_Move_MicroEngine_UI_UiAction_)), then returns the highlighted
option when confirm is pressed on one that can be chosen, else null. The press that chooses is consumed
([Consume](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Consume_System_String_)), so nothing read after the menu acts on it too; a confirm on an option that
cannot be chosen is left as it is.

```csharp
public int? Update(InputMap input, UiActions? actions = null)
```

#### Parameters

`input` [InputMap](MicroEngine.Input.InputMap.md)

`actions` [UiActions](MicroEngine.UI.UiActions.md)?

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_UI_MenuCursor_Moved"></a> Moved

Raised when a direction or [MoveTo](MicroEngine.UI.MenuCursor.md#MicroEngine_UI_MenuCursor_MoveTo_System_Int32_) changes the highlighted option (for the cursor's sound).

```csharp
public event Action? Moved
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>
