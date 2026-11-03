# Interface IInputBackend

<div data-pagefind-weight="0.3">

Namespace: [MicroEngine.Input](MicroEngine.Input.md)  
Assembly: MicroEngine.Input.dll  
Guide: [Input](../manual/input.md)  

The input part of a platform: the devices and what happens on them, delivered as events. A capability of hosts with
input ([TryGetCapability``1](MicroEngine.Platform.IPlatformHost.md#MicroEngine_Platform_IPlatformHost_TryGetCapability__1___0__)); the engine drains it once per frame, at the start
of the frame's input phase.

```csharp
public interface IInputBackend
```

## Remarks

The contract every input backend follows:

<ol><li>Events are in the order they happened; a host gathers them in its event pump and hands them over in
[Drain](MicroEngine.Input.IInputBackend.md#MicroEngine_Input_IInputBackend_Drain_System_Collections_Generic_List_MicroEngine_Input_InputEvent__), never between drains.</li><li>Every connected device is announced with [DeviceAdded](MicroEngine.Input.InputEventKind.md) before its first other
event, also those present at start; a disconnected one with [DeviceRemoved](MicroEngine.Input.InputEventKind.md) after events
releasing what it held.</li><li>Keys are positions ([Key](MicroEngine.Input.Key.md)), never characters; characters come as [Text](MicroEngine.Input.InputEventKind.md)
only while text input is on ([ITextInputControl](MicroEngine.Input.ITextInputControl.md)).</li><li>Pointer positions are in virtual canvas pixels. A backend does not synthesize mouse events from touches or
touches from the mouse.</li><li>When the window loses focus, the backend releases every key and button it reported down.</li></ol>

## Methods

### <a id="MicroEngine_Input_IInputBackend_Drain_System_Collections_Generic_List_MicroEngine_Input_InputEvent__"></a> Drain\(List<InputEvent\>\)

Moves every event since the previous call into <code class="paramref">events</code>, oldest first.

```csharp
void Drain(List<InputEvent> events)
```

#### Parameters

`events` [List](https://learn.microsoft.com/dotnet/api/system.collections.generic.list\-1)<[InputEvent](MicroEngine.Input.InputEvent.md)\>

</div>
