# Class SavesModule

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Saves](MicroEngine.Saves.md)  
Assembly: MicroEngine.Saves.dll  
Guide: [Persistence](../manual/persistence.md)  

Saves as an engine module: registers [Store](MicroEngine.Saves.SavesModule.md#MicroEngine_Saves_SavesModule_Store) as the engine's [SaveStore](MicroEngine.Core.SaveStore.md) service, so other
modules (achievements) and the game's systems find it in the services. It schedules no frame system and writes
nothing by itself: a slot is written when someone saves it. A composition that never adds the module has no store
service, and a game that never creates a store writes no file.

```csharp
public sealed class SavesModule : IEngineModule
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[SavesModule](MicroEngine.Saves.SavesModule.md)

#### Implements

[IEngineModule](MicroEngine.Kernel.IEngineModule.md)

## Examples

```csharp
var store = new SaveStore("com.example.mygame");
builder.Add(new SavesModule(store));
// later, anywhere with the services:
services.Get<SaveStore>().Save("progress", progress);
```

## Constructors

### <a id="MicroEngine_Saves_SavesModule__ctor_MicroEngine_Core_SaveStore_"></a> SavesModule\(SaveStore\)

Creates the module over <code class="paramref">store</code>.

```csharp
public SavesModule(SaveStore store)
```

#### Parameters

`store` [SaveStore](MicroEngine.Core.SaveStore.md)

#### Exceptions

 [ArgumentNullException](https://learn.microsoft.com/dotnet/api/system.argumentnullexception)

<code class="paramref">store</code> is null.

## Properties

### <a id="MicroEngine_Saves_SavesModule_Store"></a> Store

The store the module registers.

```csharp
public SaveStore Store { get; }
```

#### Property Value

 [SaveStore](MicroEngine.Core.SaveStore.md)

## Methods

### <a id="MicroEngine_Saves_SavesModule_Configure_MicroEngine_Kernel_EngineBuilder_"></a> Configure\(EngineBuilder\)

Registers [Store](MicroEngine.Saves.SavesModule.md#MicroEngine_Saves_SavesModule_Store) as the [SaveStore](MicroEngine.Core.SaveStore.md) service.

```csharp
public void Configure(EngineBuilder builder)
```

#### Parameters

`builder` [EngineBuilder](MicroEngine.Kernel.EngineBuilder.md)

</div>
