# Class AchievementSaveData

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

What is written to the save slot. Entries are keyed by achievement id.

```csharp
public sealed class AchievementSaveData
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AchievementSaveData](MicroEngine.Core.AchievementSaveData.md)

## Constructors

### <a id="MicroEngine_Core_AchievementSaveData__ctor"></a> AchievementSaveData\(\)

```csharp
public AchievementSaveData()
```

## Properties

### <a id="MicroEngine_Core_AchievementSaveData_Entries"></a> Entries

Saved state per achievement id.

```csharp
public Dictionary<string, AchievementRecord>? Entries { get; set; }
```

#### Property Value

 [Dictionary](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary\-2)<[string](https://learn.microsoft.com/dotnet/api/system.string), [AchievementRecord](MicroEngine.Core.AchievementRecord.md)\>?

### <a id="MicroEngine_Core_AchievementSaveData_Version"></a> Version

Format version of the saved data.

```csharp
public int Version { get; set; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

</div>
