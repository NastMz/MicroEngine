# Namespace MicroEngine.Saves

<div data-pagefind-weight="0.3">

Saves as an optional engine module: the module that registers a save store as a service of the engine.

Guide: [Persistence](../manual/persistence.md)  

### Classes

 [SavesModule](MicroEngine.Saves.SavesModule.md)

Saves as an engine module: registers [Store](MicroEngine.Saves.SavesModule.md#MicroEngine_Saves_SavesModule_Store) as the engine's [SaveStore](MicroEngine.Core.SaveStore.md) service, so other
modules (achievements) and the game's systems find it in the services. It schedules no frame system and writes
nothing by itself: a slot is written when someone saves it. A composition that never adds the module has no store
service, and a game that never creates a store writes no file.

</div>
