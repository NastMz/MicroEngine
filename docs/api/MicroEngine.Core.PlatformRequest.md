# Class PlatformRequest

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Backends and platforms](../manual/backends.md)  

What a host asks of a platform: the game's settings, window options and any subsystem it supplies itself. Every
option is explicit and has a documented default; none depends on how the loop advances time (that is the loop's
business only, [TimeMode](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_TimeMode)). A backend honours the supplied parts by building its audio device
with [CreateAudio](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_CreateAudio_System_Func_MicroEngine_Audio_IAudioDevice__) and its input with [CombineInput](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_CombineInput_MicroEngine_Input_IInputSource_); [Create](MicroEngine.Core.PlatformProfile.md#MicroEngine_Core_PlatformProfile_Create_MicroEngine_Core_PlatformRequest_)
checks that it did.

```csharp
public sealed record PlatformRequest : IEquatable<PlatformRequest>
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[PlatformRequest](MicroEngine.Core.PlatformRequest.md)

#### Implements

[IEquatable<PlatformRequest\>](https://learn.microsoft.com/dotnet/api/system.iequatable\-1)

## Constructors

### <a id="MicroEngine_Core_PlatformRequest__ctor_MicroEngine_Core_GameSettings_"></a> PlatformRequest\(GameSettings\)

What a host asks of a platform: the game's settings, window options and any subsystem it supplies itself. Every
option is explicit and has a documented default; none depends on how the loop advances time (that is the loop's
business only, [TimeMode](MicroEngine.Core.RunOptions.md#MicroEngine_Core_RunOptions_TimeMode)). A backend honours the supplied parts by building its audio device
with [CreateAudio](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_CreateAudio_System_Func_MicroEngine_Audio_IAudioDevice__) and its input with [CombineInput](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_CombineInput_MicroEngine_Input_IInputSource_); [Create](MicroEngine.Core.PlatformProfile.md#MicroEngine_Core_PlatformProfile_Create_MicroEngine_Core_PlatformRequest_)
checks that it did.

```csharp
public PlatformRequest(GameSettings Settings)
```

#### Parameters

`Settings` [GameSettings](MicroEngine.Core.GameSettings.md)

## Properties

### <a id="MicroEngine_Core_PlatformRequest_Audio"></a> Audio

Creates the audio device used instead of the backend's; the platform owns and disposes it.

```csharp
public Func<IAudioDevice>? Audio { get; init; }
```

#### Property Value

 [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[IAudioDevice](MicroEngine.Audio.IAudioDevice.md)\>?

### <a id="MicroEngine_Core_PlatformRequest_AudioOutput"></a> AudioOutput

Whether the backend may open a physical audio device: [Automatic](MicroEngine.Core.AudioOutput.md) (the default) and
[Device](MicroEngine.Core.AudioOutput.md) open one, [Silent](MicroEngine.Core.AudioOutput.md) opens none.

```csharp
public AudioOutput AudioOutput { get; init; }
```

#### Property Value

 [AudioOutput](MicroEngine.Core.AudioOutput.md)

### <a id="MicroEngine_Core_PlatformRequest_CaptureScale"></a> CaptureScale

Size factor of the PNGs written by [SaveScreenshot](MicroEngine.Core.IPlatform.md#MicroEngine_Core_IPlatform_SaveScreenshot_System_String_): 3 for pixel-art canvases (so their
pixels are visible), 1 for smoothly scaled ones (already high-resolution).

```csharp
public int CaptureScale { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_PlatformRequest_ExtraInput"></a> ExtraInput

Input sources read together with the backend's (an on-screen pad, a network peer, a test).

```csharp
public IReadOnlyList<IInputSource> ExtraInput { get; init; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[IInputSource](MicroEngine.Input.IInputSource.md)\>

### <a id="MicroEngine_Core_PlatformRequest_Hidden"></a> Hidden

Renders into a hidden window (captures still work). A hidden window never waits for the display.

```csharp
public bool Hidden { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_PlatformRequest_Identity"></a> Identity

The game's application identity (name, id, icon, orientation); null keeps the backend's defaults.

```csharp
public AppIdentity? Identity { get; init; }
```

#### Property Value

 [AppIdentity](MicroEngine.Core.AppIdentity.md)?

### <a id="MicroEngine_Core_PlatformRequest_Mute"></a> Mute

No sound: the backend opens no audio device.

```csharp
public bool Mute { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_PlatformRequest_OpensOwnAudio"></a> OpensOwnAudio

Whether the backend should open its own audio device: no device was supplied, no mute, and the output is not silent.

```csharp
public bool OpensOwnAudio { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_PlatformRequest_Scale"></a> Scale

Window scale; null uses [Scale](MicroEngine.Core.GameSettings.md#MicroEngine_Core_GameSettings_Scale).

```csharp
public int? Scale { get; init; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)?

### <a id="MicroEngine_Core_PlatformRequest_Settings"></a> Settings

```csharp
public GameSettings Settings { get; init; }
```

#### Property Value

 [GameSettings](MicroEngine.Core.GameSettings.md)

### <a id="MicroEngine_Core_PlatformRequest_StartGamepadsAfterFirstFrame"></a> StartGamepadsAfterFirstFrame

Whether the backend starts its gamepad subsystem only after the first frame is presented (false by default:
with the platform). Hosts that present a start screen while the game loads set it, so a slow gamepad start-up
(about a second on some Windows machines) never keeps the window from appearing; pads already connected are
reported when it starts.

```csharp
public bool StartGamepadsAfterFirstFrame { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_PlatformRequest_WaitForDisplay"></a> WaitForDisplay

Whether the frames of a visible window may wait for the display's refresh (true by default): VSync as
[VSync](MicroEngine.Core.GraphicsSettings.md#MicroEngine_Core_GraphicsSettings_VSync) asks, and frames paced to the display ([IDisplayPacing](MicroEngine.Platform.IDisplayPacing.md)). False
never waits, whatever the settings say, for checks that run as fast as they can.

```csharp
public bool WaitForDisplay { get; init; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

## Methods

### <a id="MicroEngine_Core_PlatformRequest__Clone__"></a> <Clone\>$\(\)

```csharp
public PlatformRequest <Clone>$()
```

#### Returns

 [PlatformRequest](MicroEngine.Core.PlatformRequest.md)

### <a id="MicroEngine_Core_PlatformRequest_CombineInput_MicroEngine_Input_IInputSource_"></a> CombineInput\(IInputSource\)

The input a backend reports for this request: its own source, read together with [ExtraInput](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_ExtraInput) when there are any.

```csharp
public IInputSource CombineInput(IInputSource own)
```

#### Parameters

`own` [IInputSource](MicroEngine.Input.IInputSource.md)

#### Returns

 [IInputSource](MicroEngine.Input.IInputSource.md)

### <a id="MicroEngine_Core_PlatformRequest_CreateAudio_System_Func_MicroEngine_Audio_IAudioDevice__"></a> CreateAudio\(Func<IAudioDevice\>\)

The audio device a backend uses for this request: the one the host supplied ([Audio](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_Audio)), else the
backend's own (<code class="paramref">openOwn</code>) when [OpensOwnAudio](MicroEngine.Core.PlatformRequest.md#MicroEngine_Core_PlatformRequest_OpensOwnAudio), else a silent device. The backend
owns and disposes the result.

```csharp
public IAudioDevice CreateAudio(Func<IAudioDevice> openOwn)
```

#### Parameters

`openOwn` [Func](https://learn.microsoft.com/dotnet/api/system.func\-1)<[IAudioDevice](MicroEngine.Audio.IAudioDevice.md)\>

#### Returns

 [IAudioDevice](MicroEngine.Audio.IAudioDevice.md)

### <a id="MicroEngine_Core_PlatformRequest_Deconstruct_MicroEngine_Core_GameSettings__"></a> Deconstruct\(out GameSettings\)

```csharp
public void Deconstruct(out GameSettings Settings)
```

#### Parameters

`Settings` [GameSettings](MicroEngine.Core.GameSettings.md)

### <a id="MicroEngine_Core_PlatformRequest_Equals_System_Object_"></a> Equals\(object?\)

```csharp
public override bool Equals(object? obj)
```

#### Parameters

`obj` [object](https://learn.microsoft.com/dotnet/api/system.object)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_PlatformRequest_Equals_MicroEngine_Core_PlatformRequest_"></a> Equals\(PlatformRequest?\)

```csharp
public bool Equals(PlatformRequest? other)
```

#### Parameters

`other` [PlatformRequest](MicroEngine.Core.PlatformRequest.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_PlatformRequest_GetHashCode"></a> GetHashCode\(\)

```csharp
public override int GetHashCode()
```

#### Returns

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_PlatformRequest_ToString"></a> ToString\(\)

```csharp
public override string ToString()
```

#### Returns

 [string](https://learn.microsoft.com/dotnet/api/system.string)

## Operators

### <a id="MicroEngine_Core_PlatformRequest_op_Equality_MicroEngine_Core_PlatformRequest_MicroEngine_Core_PlatformRequest_"></a> operator ==\(PlatformRequest?, PlatformRequest?\)

```csharp
public static bool operator ==(PlatformRequest? left, PlatformRequest? right)
```

#### Parameters

`left` [PlatformRequest](MicroEngine.Core.PlatformRequest.md)?

`right` [PlatformRequest](MicroEngine.Core.PlatformRequest.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_PlatformRequest_op_Inequality_MicroEngine_Core_PlatformRequest_MicroEngine_Core_PlatformRequest_"></a> operator \!=\(PlatformRequest?, PlatformRequest?\)

```csharp
public static bool operator !=(PlatformRequest? left, PlatformRequest? right)
```

#### Parameters

`left` [PlatformRequest](MicroEngine.Core.PlatformRequest.md)?

`right` [PlatformRequest](MicroEngine.Core.PlatformRequest.md)?

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
