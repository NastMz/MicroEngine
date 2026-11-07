# Struct UiClip

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.UI](MicroEngine.UI.md)  
Assembly: MicroEngine.UI.dll  
Guide: [UI widgets](../manual/ui.md)  

A clip set by [ClipTo](MicroEngine.UI.UiContext.md#MicroEngine_UI_UiContext_ClipTo_MicroEngine_Graphics_IRenderer_MicroEngine_RectF_MicroEngine_UI_UiContext__); disposing it restores the clip that was set before.

```csharp
public readonly struct UiClip : IDisposable
```

#### Implements

[IDisposable](https://learn.microsoft.com/dotnet/api/system.idisposable)

## Methods

### <a id="MicroEngine_UI_UiClip_Dispose"></a> Dispose\(\)

Restores the clip that was set before.

```csharp
public void Dispose()
```

</div>
