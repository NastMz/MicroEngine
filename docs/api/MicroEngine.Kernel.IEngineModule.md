# Interface IEngineModule

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

A part of the engine or of a game that registers itself: services, frame systems, capabilities, settings and
lifecycle handlers, all from [Configure](MicroEngine.Kernel.IEngineModule.md#MicroEngine_Kernel_IEngineModule_Configure_MicroEngine_Kernel_EngineBuilder_). Rendering, input, audio, scenes and the game itself are modules.

```csharp
public interface IEngineModule
```

## Methods

### <a id="MicroEngine_Kernel_IEngineModule_Configure_MicroEngine_Kernel_EngineBuilder_"></a> Configure\(EngineBuilder\)

Registers what the module provides on <code class="paramref">builder</code>; called once, when the module is added.

```csharp
void Configure(EngineBuilder builder)
```

#### Parameters

`builder` [EngineBuilder](MicroEngine.Kernel.EngineBuilder.md)

</div>
