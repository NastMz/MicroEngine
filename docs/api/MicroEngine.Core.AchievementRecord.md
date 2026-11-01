# Class AchievementRecord

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The saved state of one achievement.

```csharp
public sealed class AchievementRecord
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AchievementRecord](MicroEngine.Core.AchievementRecord.md)

## Constructors

### <a id="MicroEngine_Core_AchievementRecord__ctor"></a> AchievementRecord\(\)

```csharp
public AchievementRecord()
```

## Properties

### <a id="MicroEngine_Core_AchievementRecord_Progress"></a> Progress

Accumulated progress.

```csharp
public int Progress { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AchievementRecord_Unlocked"></a> Unlocked

Whether the achievement is unlocked.

```csharp
public bool Unlocked { get; set; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)
