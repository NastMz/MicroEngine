# Class AchievementTracker

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

Owns a game's achievements: the catalog, progress, unlocking and persistence. Games decide when
something happened and report it with [Unlock](MicroEngine.Core.AchievementTracker.md#MicroEngine_Core_AchievementTracker_Unlock_System_String_), [AddProgress](MicroEngine.Core.AchievementTracker.md#MicroEngine_Core_AchievementTracker_AddProgress_System_String_System_Int32_) or
[SetProgress](MicroEngine.Core.AchievementTracker.md#MicroEngine_Core_AchievementTracker_SetProgress_System_String_System_Int32_); the tracker guarantees that an achievement unlocks (and raises
[Unlocked](MicroEngine.Core.AchievementTracker.md#MicroEngine_Core_AchievementTracker_Unlocked)) exactly once, across sessions.

```csharp
public sealed class AchievementTracker
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[AchievementTracker](MicroEngine.Core.AchievementTracker.md)

## Remarks

State lives in the [SaveSlot](MicroEngine.Core.AchievementTracker.md#MicroEngine_Core_AchievementTracker_SaveSlot) slot of the game's [SaveStore](MicroEngine.Core.SaveStore.md), so each game
keeps its own file. Missing or corrupt data starts from scratch (the store keeps the corrupt file aside);
a file that exists but cannot be read is never overwritten during that session. Entries for ids that are no longer in
the catalog are kept untouched, and new catalog entries start locked. Unlocks are written at once;
plain progress is written by [Flush](MicroEngine.Core.AchievementTracker.md#MicroEngine_Core_AchievementTracker_Flush) (the game loop calls it about once a second and on exit).

## Constructors

### <a id="MicroEngine_Core_AchievementTracker__ctor_System_Collections_Generic_IEnumerable_MicroEngine_Core_Achievement__MicroEngine_Core_SaveStore_"></a> AchievementTracker\(IEnumerable<Achievement\>, SaveStore?\)

```csharp
public AchievementTracker(IEnumerable<Achievement> catalog, SaveStore? store = null)
```

#### Parameters

`catalog` [IEnumerable](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable\-1)<[Achievement](MicroEngine.Core.Achievement.md)\>

The game's achievements, in display order.

`store` SaveStore?

Where progress persists; null keeps it in memory only.

## Fields

### <a id="MicroEngine_Core_AchievementTracker_SaveSlot"></a> SaveSlot

The [SaveStore](MicroEngine.Core.SaveStore.md) slot that holds achievement state: "achievements".

```csharp
public const string SaveSlot = "achievements"
```

#### Field Value

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Properties

### <a id="MicroEngine_Core_AchievementTracker_All"></a> All

Every achievement in catalog order.

```csharp
public IReadOnlyList<AchievementStatus> All { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[AchievementStatus](MicroEngine.Core.AchievementStatus.md)\>

### <a id="MicroEngine_Core_AchievementTracker_Catalog"></a> Catalog

The achievements in display order.

```csharp
public IReadOnlyList<Achievement> Catalog { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[Achievement](MicroEngine.Core.Achievement.md)\>

### <a id="MicroEngine_Core_AchievementTracker_Count"></a> Count

Number of achievements in the catalog.

```csharp
public int Count { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_AchievementTracker_HasUnsavedChanges"></a> HasUnsavedChanges

True when progress changed since the last successful save.

```csharp
public bool HasUnsavedChanges { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AchievementTracker_UnlockedCount"></a> UnlockedCount

Number of unlocked achievements in the catalog.

```csharp
public int UnlockedCount { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

## Methods

### <a id="MicroEngine_Core_AchievementTracker_AddProgress_System_String_System_Int32_"></a> AddProgress\(string, int\)

Adds to an achievement's progress; unlocks it when the goal is reached. Returns true only on that unlock.

```csharp
public bool AddProgress(string id, int amount = 1)
```

#### Parameters

`id` [string](https://learn.microsoft.com/dotnet/api/system.string)

`amount` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AchievementTracker_Flush"></a> Flush\(\)

Writes pending progress. Failures (read-only disk, unreadable existing file) keep it in memory and
return false; the next call tries again.

```csharp
public bool Flush()
```

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AchievementTracker_Get_System_String_"></a> Get\(string\)

The current state of one achievement; progress is capped at its goal.

```csharp
public AchievementStatus Get(string id)
```

#### Parameters

`id` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [AchievementStatus](MicroEngine.Core.AchievementStatus.md)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

<code class="paramref">id</code> is not in the catalog.

### <a id="MicroEngine_Core_AchievementTracker_IsUnlocked_System_String_"></a> IsUnlocked\(string\)

Whether the achievement is unlocked.

```csharp
public bool IsUnlocked(string id)
```

#### Parameters

`id` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

<code class="paramref">id</code> is not in the catalog.

### <a id="MicroEngine_Core_AchievementTracker_Progress_System_String_"></a> Progress\(string\)

The achievement's progress, from 0 to its goal.

```csharp
public int Progress(string id)
```

#### Parameters

`id` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Exceptions

 [KeyNotFoundException](https://learn.microsoft.com/dotnet/api/system.collections.generic.keynotfoundexception)

<code class="paramref">id</code> is not in the catalog.

### <a id="MicroEngine_Core_AchievementTracker_SetProgress_System_String_System_Int32_"></a> SetProgress\(string, int\)

Reports an absolute count ("rooms visited: 7"). Progress never goes down, so recounting after a new
game cannot erase what was achieved before. Returns true only when this call unlocks it.

```csharp
public bool SetProgress(string id, int value)
```

#### Parameters

`id` [string](https://learn.microsoft.com/dotnet/api/system.string)

`value` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AchievementTracker_Unlock_System_String_"></a> Unlock\(string\)

Unlocks an achievement now. Returns true only the first time.

```csharp
public bool Unlock(string id)
```

#### Parameters

`id` [string](https://learn.microsoft.com/dotnet/api/system.string)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_AchievementTracker_Unlocked"></a> Unlocked

Raised once when an achievement becomes unlocked (never for ones loaded already unlocked).

```csharp
public event Action<Achievement>? Unlocked
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[Achievement](MicroEngine.Core.Achievement.md)\>?

</div>
