# Class InputDevices

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The state of every device, derived from the events no input layer took: which keys and buttons are held, where the
axes, the pointers and the wheel are, and which devices are connected. A press that started and ended between two
updates still reads as held until an update has sampled it ([EndSample](MicroEngine.Input.InputDevices.md#MicroEngine_Input_InputDevices_EndSample)): taps expire by updates, never
by drawn frames. Players read it through views limited to their devices ([View](MicroEngine.Input.InputDevices.md#MicroEngine_Input_InputDevices_View_System_Func_MicroEngine_Input_DeviceId_System_Boolean__)).

```csharp
public sealed class InputDevices
```

#### Inheritance

[object](https://learn.microsoft.com/dotnet/api/system.object) ← 
[InputDevices](MicroEngine.Input.InputDevices.md)

## Constructors

### <a id="MicroEngine_Input_InputDevices__ctor"></a> InputDevices\(\)

```csharp
public InputDevices()
```

## Properties

### <a id="MicroEngine_Input_InputDevices_Devices"></a> Devices

The connected devices, in the order they connected.

```csharp
public IReadOnlyList<InputDeviceInfo> Devices { get; }
```

#### Property Value

 [IReadOnlyList](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist\-1)<[InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)\>

## Methods

### <a id="MicroEngine_Input_InputDevices_Apply_MicroEngine_Input_InputEvent__"></a> Apply\(in InputEvent\)

Applies one event. An event of a device that was never announced announces it first as a generic device of the
event's kind, so a backend's own ids always work.

```csharp
public void Apply(in InputEvent e)
```

#### Parameters

`e` [InputEvent](MicroEngine.Input.InputEvent.md)

### <a id="MicroEngine_Input_InputDevices_EndSample"></a> EndSample\(\)

Ends a sample: presses that were already released stop reading as held, the wheel starts again from 0 and
lifted fingers go. Called once after every update has read the devices.

```csharp
public void EndSample()
```

### <a id="MicroEngine_Input_InputDevices_Find_MicroEngine_Input_DeviceId_"></a> Find\(DeviceId\)

The device's description, or null when it is not connected.

```csharp
public InputDeviceInfo? Find(DeviceId id)
```

#### Parameters

`id` [DeviceId](MicroEngine.Input.DeviceId.md)

#### Returns

 [InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)?

### <a id="MicroEngine_Input_InputDevices_ReleaseAll"></a> ReleaseAll\(\)

Releases everything held on every device (the window lost focus, the game was suspended).

```csharp
public void ReleaseAll()
```

### <a id="MicroEngine_Input_InputDevices_View_System_Func_MicroEngine_Input_DeviceId_System_Boolean__"></a> View\(Func<DeviceId, bool\>\)

The state of the devices <code class="paramref">owns</code> accepts, read as one polled source: a key or button is down when
any of them has it down, the axis with the largest deflection wins, the pointer is the one that moved last.

```csharp
public IInputSource View(Func<DeviceId, bool> owns)
```

#### Parameters

`owns` [Func](https://learn.microsoft.com/dotnet/api/system.func\-2)<[DeviceId](MicroEngine.Input.DeviceId.md), [bool](https://learn.microsoft.com/dotnet/api/system.boolean)\>

#### Returns

 [IInputSource](MicroEngine.Input.IInputSource.md)

### <a id="MicroEngine_Input_InputDevices_Connected"></a> Connected

Raised when a device connects (also those present at start), after it was added.

```csharp
public event Action<InputDeviceInfo>? Connected
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)\>?

### <a id="MicroEngine_Input_InputDevices_Disconnected"></a> Disconnected

Raised when a device disconnects, after it was removed.

```csharp
public event Action<InputDeviceInfo>? Disconnected
```

#### Event Type

 [Action](https://learn.microsoft.com/dotnet/api/system.action\-1)<[InputDeviceInfo](MicroEngine.Input.InputDeviceInfo.md)\>?

</div>
