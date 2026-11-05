# Class AchievementToasts

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.dll  
Guide: [Achievements](../manual/achievements.md)  

Queued "achievement unlocked" banners that slide in at the top of the screen, one at a time: the UI's
[Toasts](MicroEngine.UI.Toasts.md) showing achievements. Purely visual: they never read input, so play continues underneath.
[Game](MicroEngine.Core.Game.md) owns one when achievements are enabled and draws it above every scene.

```csharp
public sealed class AchievementToasts
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AchievementToasts](MicroEngine.UI.AchievementToasts.md)

## Constructors

### <a id="MicroEngine_UI_AchievementToasts__ctor"></a> AchievementToasts\(\)

Creates the banners; each one that leaves the screen makes the next achievement current.

```csharp
public AchievementToasts()
```

## Fields

### <a id="MicroEngine_UI_AchievementToasts_ShowTime"></a> ShowTime

Seconds each banner stays (3.4), and seconds it takes to slide in and out (0.25).

```csharp
public const float ShowTime = 3.4
```

#### Field Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_UI_AchievementToasts_SlideTime"></a> SlideTime

Seconds each banner stays (3.4), and seconds it takes to slide in and out (0.25).

```csharp
public const float SlideTime = 0.25
```

#### Field Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Properties

### <a id="MicroEngine_UI_AchievementToasts_Banners"></a> Banners

The banners themselves, a [Toasts](MicroEngine.UI.Toasts.md) of the UI.

```csharp
public Toasts Banners { get; }
```

#### Property Value

 Toasts

### <a id="MicroEngine_UI_AchievementToasts_Bottom"></a> Bottom

Distance from the bottom edge when fully shown, for games whose HUD sits at the top; null (the default) shows banners at the top.

```csharp
public float? Bottom { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)?

### <a id="MicroEngine_UI_AchievementToasts_Current"></a> Current

The banner on screen, or null.

```csharp
public Achievement? Current { get; }
```

#### Property Value

 [Achievement](MicroEngine.Core.Achievement.md)?

### <a id="MicroEngine_UI_AchievementToasts_Heading"></a> Heading

The first line of every banner queued from now on ("ACHIEVEMENT UNLOCKED" by default).

```csharp
public string Heading { get; set; }
```

#### Property Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

### <a id="MicroEngine_UI_AchievementToasts_Pending"></a> Pending

Banners waiting behind the current one.

```csharp
public int Pending { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_UI_AchievementToasts_Theme"></a> Theme

Colours of the banners.

```csharp
public UiTheme Theme { get; set; }
```

#### Property Value

 UiTheme

### <a id="MicroEngine_UI_AchievementToasts_Top"></a> Top

Distance from the top edge when fully shown.

```csharp
public float Top { get; set; }
```

#### Property Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

## Methods

### <a id="MicroEngine_UI_AchievementToasts_Draw_MicroEngine_Graphics_IRenderer_MicroEngine_Graphics_BitmapFont_"></a> Draw\(IRenderer, BitmapFont\)

Draws the current banner centred at the top of the screen (or the bottom, see [Bottom](MicroEngine.UI.AchievementToasts.md#MicroEngine_UI_AchievementToasts_Bottom)); the game calls it after the scenes.

```csharp
public void Draw(IRenderer r, BitmapFont font)
```

#### Parameters

`r` IRenderer

`font` BitmapFont

### <a id="MicroEngine_UI_AchievementToasts_Show_MicroEngine_Core_Achievement_"></a> Show\(Achievement\)

Queues a banner for <code class="paramref">achievement</code>; it shows at once when no other is on screen.

```csharp
public void Show(Achievement achievement)
```

#### Parameters

`achievement` [Achievement](MicroEngine.Core.Achievement.md)

### <a id="MicroEngine_UI_AchievementToasts_Update_System_Single_"></a> Update\(float\)

Advances the current banner; the game calls it every update.

```csharp
public void Update(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

</div>
