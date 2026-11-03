# Class AchievementsScene

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.dll  
Guide: [Achievements](../manual/achievements.md)  

Overlay listing a game's achievements: unlocked ones highlighted, pending ones dimmed, progress bars
for progressive goals. Up/down moves the cursor (the list scrolls); any of [CloseActions](MicroEngine.UI.AchievementsScene.md#MicroEngine_UI_AchievementsScene_CloseActions) closes it.

```csharp
public sealed class AchievementsScene : Scene, IStateHashed
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[Scene](MicroEngine.Scenes.Scene.md) ← 
[AchievementsScene](MicroEngine.UI.AchievementsScene.md)

#### Implements

IStateHashed

#### Inherited Members

[Scene.Enter\(\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Enter), 
[Scene.Exit\(\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Exit), 
[Scene.Resume\(\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Resume), 
[Scene.Describe\(\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Describe), 
[Scene.HashState\(StateHasher\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_HashState\_MicroEngine\_Kernel\_StateHasher\_), 
[Scene.Update\(float\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Update\_System\_Single\_), 
[Scene.Draw\(IRenderer\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Draw\_MicroEngine\_Graphics\_IRenderer\_), 
[Scene.DrawUi\(IRenderer\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_DrawUi\_MicroEngine\_Graphics\_IRenderer\_), 
[Scene.UiContains\(Vector2\)](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_UiContains\_System\_Numerics\_Vector2\_), 
[Scene.Game](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Game), 
[Scene.Input](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Input), 
[Scene.Audio](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Audio), 
[Scene.Font](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Font), 
[Scene.Assets](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Assets), 
[Scene.Coroutines](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Coroutines), 
[Scene.Tweens](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Tweens), 
[Scene.Age](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Age), 
[Scene.IsOverlay](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_IsOverlay), 
[Scene.BlocksUpdateBelow](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_BlocksUpdateBelow), 
[Scene.Objects](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_Objects), 
[Scene.TouchActions](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_TouchActions), 
[Scene.WorldCamera](MicroEngine.Scenes.Scene.md\#MicroEngine\_Scenes\_Scene\_WorldCamera), 
[object.GetType\(\)](https://learn.microsoft.com/dotnet/api/system.object.gettype), 
[object.ToString\(\)](https://learn.microsoft.com/dotnet/api/system.object.tostring), 
[object.Equals\(object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\)), 
[object.Equals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.equals\#system\-object\-equals\(system\-object\-system\-object\)), 
[object.ReferenceEquals\(object?, object?\)](https://learn.microsoft.com/dotnet/api/system.object.referenceequals), 
[object.GetHashCode\(\)](https://learn.microsoft.com/dotnet/api/system.object.gethashcode)

## Constructors

### <a id="MicroEngine_UI_AchievementsScene__ctor_MicroEngine_Core_AchievementTracker_MicroEngine_UI_UiTheme_"></a> AchievementsScene\(AchievementTracker, UiTheme\)

Overlay listing a game's achievements: unlocked ones highlighted, pending ones dimmed, progress bars
for progressive goals. Up/down moves the cursor (the list scrolls); any of [CloseActions](MicroEngine.UI.AchievementsScene.md#MicroEngine_UI_AchievementsScene_CloseActions) closes it.

```csharp
public AchievementsScene(AchievementTracker tracker, UiTheme theme)
```

#### Parameters

`tracker` [AchievementTracker](MicroEngine.Core.AchievementTracker.md)

`theme` [UiTheme](MicroEngine.UI.UiTheme.md)

## Properties

### <a id="MicroEngine_UI_AchievementsScene_CloseActions"></a> CloseActions

Actions that close the screen; null (the default) is the map's cancel, pause and confirm actions. The cursor moves
with the map's up and down actions ([Names](MicroEngine.Input.InputMap.md#MicroEngine_Input_InputMap_Names)).

```csharp
public string[]? CloseActions { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)\[\]?

### <a id="MicroEngine_UI_AchievementsScene_Dim"></a> Dim

Screen tint drawn over the scene below.

```csharp
public Color Dim { get; init; }
```

#### Property Value

 Color

### <a id="MicroEngine_UI_AchievementsScene_IsOverlay"></a> IsOverlay

Always true: the scene below stays visible, dimmed by [Dim](MicroEngine.UI.AchievementsScene.md#MicroEngine_UI_AchievementsScene_Dim).

```csharp
public override bool IsOverlay { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_UI_AchievementsScene_LockedLabel"></a> LockedLabel

Text shown for locked achievements.

```csharp
public string LockedLabel { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_AchievementsScene_Selected"></a> Selected

Index of the highlighted achievement in catalog order.

```csharp
public int Selected { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_AchievementsScene_Title"></a> Title

The heading of the list ("ACHIEVEMENTS" by default).

```csharp
public string Title { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_AchievementsScene_UnlockedLabel"></a> UnlockedLabel

Text shown for unlocked achievements.

```csharp
public string UnlockedLabel { get; init; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Methods

### <a id="MicroEngine_UI_AchievementsScene_Draw_MicroEngine_Graphics_IRenderer_"></a> Draw\(IRenderer\)

Draws the dimmed backdrop, the panel and the visible part of the list.

```csharp
public override void Draw(IRenderer r)
```

#### Parameters

`r` [IRenderer](MicroEngine.Graphics.IRenderer.md)

### <a id="MicroEngine_UI_AchievementsScene_Update_System_Single_"></a> Update\(float\)

Moves the cursor, or pops the scene when a close action is pressed.

```csharp
public override void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_AchievementsScene_Closed"></a> Closed

Raised when the screen closes (for a sound).

```csharp
public event Action? Closed
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

### <a id="MicroEngine_UI_AchievementsScene_Moved"></a> Moved

Raised when the cursor moves (for a sound).

```csharp
public event Action? Moved
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action)?

</div>
