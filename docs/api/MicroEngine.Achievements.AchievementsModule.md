# Class AchievementsModule

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Achievements](MicroEngine.Achievements.md)  
Assembly: MicroEngine.Achievements.dll  
Guide: [Achievements](../manual/achievements.md)  

Achievements as an engine module: registers [Tracker](MicroEngine.Achievements.AchievementsModule.md#MicroEngine_Achievements_AchievementsModule_Tracker) as the engine's [AchievementTracker](MicroEngine.Core.AchievementTracker.md)
service and schedules [SystemName](MicroEngine.Achievements.AchievementsModule.md#MicroEngine_Achievements_AchievementsModule_SystemName) in [PostFixed](MicroEngine.Kernel.FramePhase.md), which writes pending progress
about once a second of simulated time ([FlushInterval](MicroEngine.Achievements.AchievementsModule.md#MicroEngine_Achievements_AchievementsModule_FlushInterval); unlocks are written at once by the tracker) and
raises [Stepped](MicroEngine.Achievements.AchievementsModule.md#MicroEngine_Achievements_AchievementsModule_Stepped) every step; progress is written too when the platform suspends the game. A composition
that never adds the module has no tracker, no system and no achievement file. The <code>Game</code> façade adds it when the game
declares achievements (<code>UseAchievements</code> before the engine is composed).

```csharp
public sealed class AchievementsModule : IEngineModule
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AchievementsModule](MicroEngine.Achievements.AchievementsModule.md)

#### Implements

[IEngineModule](MicroEngine.Kernel.IEngineModule.md)

## Examples

```csharp
var tracker = new AchievementTracker(catalog, new SaveStore("com.example.mygame"));
builder.Add(new AchievementsModule(tracker));
```

## Constructors

### <a id="MicroEngine_Achievements_AchievementsModule__ctor_MicroEngine_Core_AchievementTracker_"></a> AchievementsModule\(AchievementTracker\)

Creates the module over <code class="paramref">tracker</code>.

```csharp
public AchievementsModule(AchievementTracker tracker)
```

#### Parameters

`tracker` [AchievementTracker](MicroEngine.Core.AchievementTracker.md)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">tracker</code> is null.

## Fields

### <a id="MicroEngine_Achievements_AchievementsModule_FlushInterval"></a> FlushInterval

Seconds of simulated time between writes of plain progress: 1.

```csharp
public const float FlushInterval = 1
```

#### Field Value

 [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Achievements_AchievementsModule_SystemName"></a> SystemName

The name of the module's frame system: "engine.achievements".

```csharp
public const string SystemName = "engine.achievements"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Achievements_AchievementsModule_Tracker"></a> Tracker

The tracker the module registers and flushes.

```csharp
public AchievementTracker Tracker { get; }
```

#### Property Value

 [AchievementTracker](MicroEngine.Core.AchievementTracker.md)

## Methods

### <a id="MicroEngine_Achievements_AchievementsModule_Configure_MicroEngine_Kernel_EngineBuilder_"></a> Configure\(EngineBuilder\)

Registers the tracker, schedules [SystemName](MicroEngine.Achievements.AchievementsModule.md#MicroEngine_Achievements_AchievementsModule_SystemName) (before <code>engine.input.end</code> when the composition has
it, the place it always had in a game's step) and writes progress when the game is suspended.

```csharp
public void Configure(EngineBuilder builder)
```

#### Parameters

`builder` [EngineBuilder](MicroEngine.Kernel.EngineBuilder.md)

### <a id="MicroEngine_Achievements_AchievementsModule_Step_System_Single_"></a> Step\(float\)

Advances the module by one step of <code class="paramref">dt</code> seconds: raises [Stepped](MicroEngine.Achievements.AchievementsModule.md#MicroEngine_Achievements_AchievementsModule_Stepped), then writes progress when [FlushInterval](MicroEngine.Achievements.AchievementsModule.md#MicroEngine_Achievements_AchievementsModule_FlushInterval) has passed.

```csharp
public void Step(float dt)
```

#### Parameters

`dt` [float](https://learn.microsoft.com/dotnet/api/system.single)

### <a id="MicroEngine_Achievements_AchievementsModule_Stepped"></a> Stepped

Raised every fixed step with the step's length in seconds, before progress is written when due (the game's unlock banners advance on it).

```csharp
public event Action<float>? Stepped
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[float](https://learn.microsoft.com/dotnet/api/system.single)\>?

</div>
