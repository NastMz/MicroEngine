# Interface IScreenEffects

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

The window stage of a platform: where the finished frame (the letterbox, the canvas scaled into its frame and the UI
view over it) can go through a post-processing chain at the window's resolution before it is shown. A capability
([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)) of the SDL and raylib platforms and of the headless
platform; the game sets it every frame from [ScreenPostProcess](MicroEngine.Core.Game.md#MicroEngine_Core_Game_ScreenPostProcess).

```csharp
public interface IScreenEffects
```

## Properties

### <a id="MicroEngine_Core_IScreenEffects_ScreenEffects"></a> ScreenEffects

The chain the next frames are presented through, or null to present them as they are. The platform runs it; the
game that set it owns it. While it is set and active, captures show the window as it was presented.

```csharp
PostProcess? ScreenEffects { get; set; }
```

#### Property Value

 [PostProcess](MicroEngine.Graphics.PostProcess.md)?

</div>
