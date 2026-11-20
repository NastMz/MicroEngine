# Enum SaveLoadStatus

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.Saves.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

What [Load``1](MicroEngine.Core.SaveStore.md#MicroEngine_Core_SaveStore_Load__1_System_String_) found in a slot.

```csharp
public enum SaveLoadStatus
```

## Fields

`Corrupt = 5` 

Not a save: invalid JSON, not a JSON object, a version that is not a whole number from 1, or a save of
            the current version that does not fit the saved type. The store has set the data aside; a new save can be written.



`Loaded = 1` 

A save of the current version.



`Migrated = 2` 

A save of an older version, brought up to the current one in memory. The slot still holds the older save; the
next save replaces it, after the store has kept a copy of it.



`MigrationFailed = 4` 

An older save that this build failed to bring up to date: a step threw, or what the steps left does not fit the
saved type ([Error](MicroEngine.Core.md) says which). It is left as it is, and the store refuses to
save over it.



`Missing = 0` 

The slot holds nothing: start fresh.



`TooNew = 3` 

A save written by a newer version of the game than this one. It is left as it is, and the store refuses to save
over it.



`Unreadable = 6` 

The slot exists but the medium could not read it. The store refuses to save over it.

</div>
