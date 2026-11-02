# Interface IDisplayControl

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Core](MicroEngine.Core.md)  
Assembly: MicroEngine.dll  
Guide: [Game lifecycle and time](../manual/lifecycle.md)  

What a platform can change about its window and presentation while the game runs ([Screen](MicroEngine.Core.Game.md#MicroEngine_Core_Game_Screen) uses it).
Platforms without a window (headless) or whose host owns these choices (browsers, phones) leave it out or report
that a change did not apply.

```csharp
public interface IDisplayControl
```

## Properties

### <a id="MicroEngine_Core_IDisplayControl_CurrentDisplay"></a> CurrentDisplay

Index in [Displays](MicroEngine.Core.IDisplayControl.md#MicroEngine_Core_IDisplayControl_Displays) of the display the window is on; -1 when unknown.

```csharp
int CurrentDisplay { get; }
```

#### Property Value

 [int](https://learn.microsoft.com/dotnet/api/system.int32)

### <a id="MicroEngine_Core_IDisplayControl_Displays"></a> Displays

The displays the system reports, in its order.

```csharp
IReadOnlyList<DisplayInfo> Displays { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[DisplayInfo](MicroEngine.Core.DisplayInfo.md)\>

### <a id="MicroEngine_Core_IDisplayControl_VSync"></a> VSync

Whether frames wait for the display's refresh.

```csharp
bool VSync { get; }
```

#### Property Value

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_IDisplayControl_WindowMode"></a> WindowMode

The window's current mode.

```csharp
WindowMode WindowMode { get; }
```

#### Property Value

 [WindowMode](MicroEngine.Core.WindowMode.md)

## Methods

### <a id="MicroEngine_Core_IDisplayControl_MoveToDisplay_System_Int32_"></a> MoveToDisplay\(int\)

Moves the window (and full screen) to display <code class="paramref">index</code>; returns whether it applied.

```csharp
bool MoveToDisplay(int index)
```

#### Parameters

`index` [int](https://learn.microsoft.com/dotnet/api/system.int32)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_IDisplayControl_SetVSync_System_Boolean_"></a> SetVSync\(bool\)

Turns VSync on or off; returns whether [VSync](MicroEngine.Core.IDisplayControl.md#MicroEngine_Core_IDisplayControl_VSync) now equals <code class="paramref">on</code>.

```csharp
bool SetVSync(bool on)
```

#### Parameters

`on` [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

### <a id="MicroEngine_Core_IDisplayControl_SetWindowMode_MicroEngine_Core_WindowMode_"></a> SetWindowMode\(WindowMode\)

Changes the window's mode; returns whether it applied.

```csharp
bool SetWindowMode(WindowMode mode)
```

#### Parameters

`mode` [WindowMode](MicroEngine.Core.WindowMode.md)

#### Returns

 [bool](https://learn.microsoft.com/dotnet/api/system.boolean)

</div>
