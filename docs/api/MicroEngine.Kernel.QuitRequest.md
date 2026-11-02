# Class QuitRequest

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Kernel](MicroEngine.Kernel.md)  
Assembly: MicroEngine.Kernel.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

A request to quit that came from outside the game (the window's close button, the system), passed to
[QuitRequested](MicroEngine.Kernel.IEngineLifecycle.md#MicroEngine_Kernel_IEngineLifecycle_QuitRequested). A handler that must keep the game running (unsaved work, a confirmation
dialog) calls [Veto](MicroEngine.Kernel.QuitRequest.md#MicroEngine_Kernel_QuitRequest_Veto); the game can quit later on its own.

```csharp
public sealed class QuitRequest
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[QuitRequest](MicroEngine.Kernel.QuitRequest.md)

## Constructors

### <a id="MicroEngine_Kernel_QuitRequest__ctor"></a> QuitRequest\(\)

```csharp
public QuitRequest()
```

## Properties

### <a id="MicroEngine_Kernel_QuitRequest_Vetoed"></a> Vetoed

Whether a handler vetoed the request.

```csharp
public bool Vetoed { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Kernel_QuitRequest_Veto"></a> Veto\(\)

Keeps the game running.

```csharp
public void Veto()
```

</div>
